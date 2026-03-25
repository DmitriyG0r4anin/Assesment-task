package ingestor

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"net/http/httptest"
	"testing"

	"github.com/IBM/sarama"
	"github.com/dataingestor/config"
	"github.com/dataingestor/models"
)

// fakeProducer is a minimal fake implementing sarama.SyncProducer for tests.
type fakeProducer struct {
	lastTopic string
	lastValue []byte
	sendCount int
	fail      bool
}

func (p *fakeProducer) SendMessage(msg *sarama.ProducerMessage) (partition int32, offset int64, err error) {
	if msg == nil {
		return 0, 0, fmt.Errorf("nil message")
	}
	// Extract bytes from sarama.ByteEncoder if present.
	if msg.Value != nil {
		if be, ok := msg.Value.(sarama.ByteEncoder); ok {
			p.lastValue = []byte(be)
		} else {
			// Unsupported encoder type in tests — record nil to indicate no payload captured.
			p.lastValue = nil
		}
	} else {
		p.lastValue = nil
	}

	p.lastTopic = msg.Topic
	p.sendCount++
	if p.fail {
		return 0, 0, fmt.Errorf("producer error")
	}
	return 1, int64(p.sendCount), nil
}

func (p *fakeProducer) SendMessages(msgs []*sarama.ProducerMessage) error {
	if len(msgs) == 0 {
		return nil
	}
	// capture the last message for assertions
	last := msgs[len(msgs)-1]
	if last != nil && last.Value != nil {
		if be, ok := last.Value.(sarama.ByteEncoder); ok {
			p.lastValue = []byte(be)
		} else {
			p.lastValue = nil
		}
		p.lastTopic = last.Topic
		p.sendCount += len(msgs)
	}
	return nil
}

func (p *fakeProducer) Close() error { return nil }

// Helper to assert equality and fail the test with context.
func mustEqual[T comparable](t *testing.T, name string, want, got T) {
	t.Helper()
	if want != got {
		t.Fatalf("%s: want %v, got %v", name, want, got)
	}
}

func TestValidateResponse(t *testing.T) {
	cases := []struct {
		name      string
		body      []byte
		wantErr   bool
		wantRetry bool
	}{
		{"empty", []byte(""), true, true},
		{"whitespace", []byte("   \n\t "), true, true},
		{"emptyArray", []byte("[]"), true, true},
		{"null", []byte("null"), true, true},
		{"malformed", []byte("{invalid"), true, true},
		{"upstreamError", []byte(`{"error":"boom"}`), true, true},
		{"valid", []byte(`[{"type":"air_quality","name":"s1","payload":{"co2":10,"pm25":5,"humidity":50}}]`), false, false},
	}

	for _, tc := range cases {
		t.Run(tc.name, func(t *testing.T) {
			validated, err, retry := validateResponse(tc.body)
			if tc.wantErr {
				if err == nil {
					t.Fatalf("%s: expected error but got nil", tc.name)
				}
			} else {
				if err != nil {
					t.Fatalf("%s: unexpected error: %v", tc.name, err)
				}
				if string(validated) != string(tc.body) {
					t.Fatalf("%s: validated mismatch; want original body back", tc.name)
				}
			}
			mustEqual(t, "retry", tc.wantRetry, retry)
		})
	}
}

func TestDoRequest_OK_and_Failures(t *testing.T) {
	// Handler that checks the X-Api-Key and returns different statuses based on key.
	handler := http.NewServeMux()
	handler.HandleFunc("/meters", func(w http.ResponseWriter, r *http.Request) {
		key := r.Header.Get("X-Api-Key")
		if key == "" {
			http.Error(w, "missing key", http.StatusUnauthorized)
			return
		}
		switch key {
		case "good":
			w.Header().Set("Content-Type", "application/json")
			_, _ = w.Write([]byte(`[{"type":"motion","name":"m1","payload":{"motionDetected":true}}]`))
		case "notfound":
			http.Error(w, "not found", http.StatusNotFound)
		case "rate":
			http.Error(w, "rate limited", http.StatusTooManyRequests)
		default:
			http.Error(w, "bad", http.StatusBadRequest)
		}
	})

	srv := httptest.NewServer(handler)
	defer srv.Close()

	// Helper to build a DataIngestor configured against the test server.
	makeIngestor := func(apiKey string) *DataIngestor {
		return &DataIngestor{
			Config: config.Config{
				WeakAppURL:    srv.URL,
				WeakAppAPIKey: apiKey,
				// timeouts/backoffs not used directly in doRequest
			},
			Client:   srv.Client(),
			Producer: &fakeProducer{},
		}
	}

	t.Run("OK", func(t *testing.T) {
		d := makeIngestor("good")
		data, err, retry := d.doRequest(context.Background())
		if err != nil {
			t.Fatalf("unexpected error: %v", err)
		}
		if retry {
			t.Fatalf("unexpected retry=true on success")
		}
		// Ensure response contains expected JSON array
		var items []models.ResponseItem
		if err := json.Unmarshal(data, &items); err != nil {
			t.Fatalf("failed to unmarshal data: %v", err)
		}
		mustEqual(t, "items length", 1, len(items))
	})

	t.Run("Unauthorized", func(t *testing.T) {
		d := makeIngestor("")
		_, err, retry := d.doRequest(context.Background())
		if err == nil {
			t.Fatalf("expected error for unauthorized")
		}
		// doRequest returns true for retry on network/transient errors; for 401 it returns false.
		mustEqual(t, "retry", false, retry)
	})

	t.Run("NotFound_Retry", func(t *testing.T) {
		d := makeIngestor("notfound")
		_, err, retry := d.doRequest(context.Background())
		if err == nil {
			t.Fatalf("expected error for not found")
		}
		mustEqual(t, "retry", true, retry)
	})

	t.Run("RateLimit_Retry", func(t *testing.T) {
		d := makeIngestor("rate")
		_, err, retry := d.doRequest(context.Background())
		if err == nil {
			t.Fatalf("expected error for rate limited")
		}
		mustEqual(t, "retry", true, retry)
	})
}

func TestSendToKafka(t *testing.T) {
	fp := &fakeProducer{}
	d := &DataIngestor{
		Config: config.Config{
			KafkaTopic: "test-topic",
		},
		Producer: fp,
	}

	payload := []byte(`{"hello":"world"}`)
	if err := d.sendToKafka(payload); err != nil {
		t.Fatalf("sendToKafka returned error: %v", err)
	}

	mustEqual(t, "topic", "test-topic", fp.lastTopic)
	mustEqual(t, "value", string(payload), string(fp.lastValue))
}

func TestHandleItem_PublishesExpectedMessages(t *testing.T) {
	fp := &fakeProducer{}
	d := &DataIngestor{
		Config: config.Config{
			KafkaTopic: "topic-x",
		},
		Producer: fp,
	}

	// Air quality item
	aqPayload := models.AirQualityPayload{CO2: 400, PM25: 12, Humidity: 45}
	aqBytes, _ := json.Marshal(aqPayload)
	item := models.ResponseItem{
		Type:    "air_quality",
		Name:    "aq-sensor-1",
		Payload: json.RawMessage(aqBytes),
	}
	d.handleItem(item)

	if fp.sendCount != 1 {
		t.Fatalf("expected 1 message sent, got %d", fp.sendCount)
	}

	var out models.OutgoingMessage
	if err := json.Unmarshal(fp.lastValue, &out); err != nil {
		t.Fatalf("failed to unmarshal outgoing message: %v", err)
	}
	mustEqual(t, "out.Type", "air_quality", out.Type)
	mustEqual(t, "out.Name", "aq-sensor-1", out.Name)

	// Payload inside OutgoingMessage is of type interface{} after unmarshal; re-marshal to parse into struct.
	payloadBytes, _ := json.Marshal(out.Payload)
	var decodedAQ models.AirQualityPayload
	if err := json.Unmarshal(payloadBytes, &decodedAQ); err != nil {
		t.Fatalf("failed to decode payload into AirQualityPayload: %v", err)
	}
	mustEqual(t, "co2", aqPayload.CO2, decodedAQ.CO2)
	mustEqual(t, "pm25", aqPayload.PM25, decodedAQ.PM25)
	mustEqual(t, "humidity", aqPayload.Humidity, decodedAQ.Humidity)

	// Motion item
	fp.sendCount = 0
	motionPayload := models.MotionPayload{MotionDetected: true}
	mpb, _ := json.Marshal(motionPayload)
	item2 := models.ResponseItem{
		Type:    "motion",
		Name:    "motion-1",
		Payload: json.RawMessage(mpb),
	}
	d.handleItem(item2)
	if fp.sendCount != 1 {
		t.Fatalf("expected 1 message sent for motion, got %d", fp.sendCount)
	}
	var out2 models.OutgoingMessage
	if err := json.Unmarshal(fp.lastValue, &out2); err != nil {
		t.Fatalf("failed to unmarshal outgoing motion message: %v", err)
	}
	mustEqual(t, "out2.Type", "motion", out2.Type)
	mustEqual(t, "out2.Name", "motion-1", out2.Name)
	payloadBytes2, _ := json.Marshal(out2.Payload)
	var decodedMotion models.MotionPayload
	if err := json.Unmarshal(payloadBytes2, &decodedMotion); err != nil {
		t.Fatalf("failed to decode motion payload: %v", err)
	}
	mustEqual(t, "motionDetected", motionPayload.MotionDetected, decodedMotion.MotionDetected)

	// Energy item
	fp.sendCount = 0
	energyPayload := models.EnergyPayload{Energy: 12.34}
	epb, _ := json.Marshal(energyPayload)
	item3 := models.ResponseItem{
		Type:    "energy",
		Name:    "energy-1",
		Payload: json.RawMessage(epb),
	}
	d.handleItem(item3)
	if fp.sendCount != 1 {
		t.Fatalf("expected 1 message sent for energy, got %d", fp.sendCount)
	}
	var out3 models.OutgoingMessage
	if err := json.Unmarshal(fp.lastValue, &out3); err != nil {
		t.Fatalf("failed to unmarshal outgoing energy message: %v", err)
	}
	mustEqual(t, "out3.Type", "energy", out3.Type)
	mustEqual(t, "out3.Name", "energy-1", out3.Name)
	payloadBytes3, _ := json.Marshal(out3.Payload)
	var decodedEnergy models.EnergyPayload
	if err := json.Unmarshal(payloadBytes3, &decodedEnergy); err != nil {
		t.Fatalf("failed to decode energy payload: %v", err)
	}
	// Use a delta for float comparison
	if !(decodedEnergy.Energy > 12.33 && decodedEnergy.Energy < 12.35) {
		t.Fatalf("energy mismatch: want ~12.34 got %v", decodedEnergy.Energy)
	}

	// Unsupported type should not send a message
	fp.sendCount = 0
	item4 := models.ResponseItem{
		Type:    "unsupported_type",
		Name:    "u1",
		Payload: json.RawMessage(`{"x":1}`),
	}
	d.handleItem(item4)
	mustEqual(t, "unsupported send count", 0, fp.sendCount)
}
