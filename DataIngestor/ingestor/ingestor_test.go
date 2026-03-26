package ingestor

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"net/http"
	"net/http/httptest"
	"testing"
	"time"

	"github.com/IBM/sarama"
	"github.com/dataingestor/config"
	"github.com/dataingestor/models"
)

// minimal fake implementing sarama.SyncProducer for tests.
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
	if msg.Value != nil {
		if be, ok := msg.Value.(sarama.ByteEncoder); ok {
			p.lastValue = []byte(be)
		} else {
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

	makeIngestor := func(apiKey string) *DataIngestor {
		return &DataIngestor{
			Config: config.Config{
				WeakAppURL:    srv.URL,
				WeakAppAPIKey: apiKey,
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

	payloadBytes, _ := json.Marshal(out.Payload)
	var decodedAQ models.AirQualityPayload
	if err := json.Unmarshal(payloadBytes, &decodedAQ); err != nil {
		t.Fatalf("failed to decode payload into AirQualityPayload: %v", err)
	}
	mustEqual(t, "co2", aqPayload.CO2, decodedAQ.CO2)
	mustEqual(t, "pm25", aqPayload.PM25, decodedAQ.PM25)
	mustEqual(t, "humidity", aqPayload.Humidity, decodedAQ.Humidity)

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

	if !(decodedEnergy.Energy > 12.33 && decodedEnergy.Energy < 12.35) {
		t.Fatalf("energy mismatch: want ~12.34 got %v", decodedEnergy.Energy)
	}

	fp.sendCount = 0
	item4 := models.ResponseItem{
		Type:    "unsupported_type",
		Name:    "u1",
		Payload: json.RawMessage(`{"x":1}`),
	}
	d.handleItem(item4)
	mustEqual(t, "unsupported send count", 0, fp.sendCount)
}

func TestSendToKafka_Failure(t *testing.T) {
	fp := &fakeProducer{fail: true}
	d := &DataIngestor{
		Config:   config.Config{KafkaTopic: "topic-x"},
		Producer: fp,
	}

	err := d.sendToKafka([]byte(`{"x":1}`))
	if err == nil {
		t.Fatalf("expected error from sendToKafka when producer fails, got nil")
	}
}

func TestFetchDataFromWeakApp_RetryThenSuccess(t *testing.T) {
	callCount := 0
	mux := http.NewServeMux()
	mux.HandleFunc("/meters", func(w http.ResponseWriter, r *http.Request) {
		callCount++
		if callCount == 1 {
			http.Error(w, "server error", http.StatusInternalServerError)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		_, _ = w.Write([]byte(`[{"type":"motion","name":"m1","payload":{"motionDetected":true}}]`))
	})

	srv := httptest.NewServer(mux)
	defer srv.Close()

	d := &DataIngestor{
		Config: config.Config{
			WeakAppURL:     srv.URL,
			WeakAppAPIKey:  "key",
			InitialBackoff: 1 * time.Millisecond,
			MaxBackoff:     2 * time.Millisecond,
		},
		Client:   srv.Client(),
		Producer: &fakeProducer{},
	}

	data, err := d.fetchDataFromWeakApp(context.Background())
	if err != nil {
		t.Fatalf("expected success after retry, got error: %v", err)
	}
	var items []map[string]any
	if err := json.Unmarshal(data, &items); err != nil {
		t.Fatalf("failed to unmarshal returned data: %v", err)
	}
	if len(items) != 1 {
		t.Fatalf("expected 1 item after success, got %d", len(items))
	}
	if callCount < 2 {
		t.Fatalf("expected at least one retry (callCount>=2), got %d", callCount)
	}
}

func TestFetchDataFromWeakApp_ContextCanceled(t *testing.T) {
	mux := http.NewServeMux()
	mux.HandleFunc("/meters", func(w http.ResponseWriter, r *http.Request) {
		time.Sleep(50 * time.Millisecond)
		w.Header().Set("Content-Type", "application/json")
		_, _ = w.Write([]byte(`[{"type":"motion","name":"m1","payload":{"motionDetected":true}}]`))
	})

	srv := httptest.NewServer(mux)
	defer srv.Close()

	d := &DataIngestor{
		Config: config.Config{
			WeakAppURL:     srv.URL,
			WeakAppAPIKey:  "key",
			InitialBackoff: 1 * time.Millisecond,
			MaxBackoff:     2 * time.Millisecond,
		},
		Client:   srv.Client(),
		Producer: &fakeProducer{},
	}

	ctx, cancel := context.WithCancel(context.Background())
	cancel()

	_, err := d.fetchDataFromWeakApp(ctx)
	if err == nil {
		t.Fatalf("expected error when context is canceled, got nil")
	}
	if !errors.Is(err, context.Canceled) {
		t.Fatalf("expected context.Canceled, got: %v", err)
	}
}

func TestPoll_ProcessesItems(t *testing.T) {
	mux := http.NewServeMux()
	mux.HandleFunc("/meters", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")

		_, _ = w.Write([]byte(`[
			{"type":"air_quality","name":"aq1","payload":{"co2":100,"pm25":10,"humidity":40}},
			{"type":"motion","name":"m2","payload":{"motionDetected":false}},
			{"type":"energy","name":"e1","payload":{"energy":7.5}}
		]`))
	})

	srv := httptest.NewServer(mux)
	defer srv.Close()

	fp := &fakeProducer{}
	d := &DataIngestor{
		Config: config.Config{
			WeakAppURL:     srv.URL,
			WeakAppAPIKey:  "key",
			InitialBackoff: 1 * time.Millisecond,
			MaxBackoff:     2 * time.Millisecond,
		},
		Client:   srv.Client(),
		Producer: fp,
	}

	d.poll(context.Background())

	mustEqual(t, "sent messages", 3, fp.sendCount)
}

func TestNewDataIngestor_NoBrokers_ReturnsError(t *testing.T) {
	cfg := config.Config{
		KafkaBrokers: nil,
	}
	_, err := NewDataIngestor(cfg)
	if err == nil {
		t.Fatalf("expected error when creating NewDataIngestor with no brokers, got nil")
	}
}

func TestClose_WithAndWithoutProducer(t *testing.T) {
	d := &DataIngestor{}

	if err := d.Close(); err != nil {
		t.Fatalf("expected nil error when producer is nil, got %v", err)
	}

	fp := &fakeProducer{}
	d.Producer = fp
	if err := d.Close(); err != nil {
		t.Fatalf("expected nil error when closing fake producer, got %v", err)
	}
}

func TestRun_StopsOnContextCancel(t *testing.T) {
	mux := http.NewServeMux()
	mux.HandleFunc("/meters", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		_, _ = w.Write([]byte(`[{\"type\":\"motion\",\"name\":\"m1\",\"payload\":{\"motionDetected\":true}}]`))
	})
	srv := httptest.NewServer(mux)
	defer srv.Close()

	fp := &fakeProducer{}
	d := &DataIngestor{
		Config: config.Config{
			WeakAppURL:    srv.URL,
			WeakAppAPIKey: "k",
			KafkaTopic:    "topic-run",
			PollInterval:  10 * time.Millisecond,
		},
		Client:   srv.Client(),
		Producer: fp,
	}

	ctx, cancel := context.WithTimeout(context.Background(), 100*time.Millisecond)
	defer cancel()

	errCh := make(chan error, 1)
	go func() {
		errCh <- d.Run(ctx)
	}()

	err := <-errCh
	if err == nil {
		t.Fatalf("expected error (context.Done) from Run, got nil")
	}
	if !errors.Is(err, context.DeadlineExceeded) && !errors.Is(err, context.Canceled) {
		t.Fatalf("expected context error from Run, got: %v", err)
	}
}

func TestHandleItem_UnmarshalErrorsAndPublishFailure(t *testing.T) {
	fp := &fakeProducer{fail: true}
	d := &DataIngestor{
		Config:   config.Config{KafkaTopic: "topic-x"},
		Producer: fp,
	}

	item := models.ResponseItem{Type: "air_quality", Name: "a1", Payload: json.RawMessage(`invalid`)}
	d.handleItem(item)
	mustEqual(t, "sendCount after invalid payload", 0, fp.sendCount)

	validAQ := models.AirQualityPayload{CO2: 1, PM25: 2, Humidity: 3}
	b, _ := json.Marshal(validAQ)
	item2 := models.ResponseItem{Type: "air_quality", Name: "a2", Payload: json.RawMessage(b)}
	d.handleItem(item2)

	mustEqual(t, "sendCount after publish failure", 1, fp.sendCount)
}

func TestDoRequest_StatusCodes(t *testing.T) {
	mux := http.NewServeMux()
	mux.HandleFunc("/meters", func(w http.ResponseWriter, r *http.Request) {
		key := r.Header.Get("X-Api-Key")
		switch key {
		case "401":
			http.Error(w, "unauthorized", http.StatusUnauthorized)
		case "403":
			http.Error(w, "forbidden", http.StatusForbidden)
		case "404":
			http.Error(w, "notfound", http.StatusNotFound)
		case "429":
			http.Error(w, "rate", http.StatusTooManyRequests)
		default:
			w.Header().Set("Content-Type", "application/json")
			_, _ = w.Write([]byte(`[{"type":"motion","name":"m1","payload":{"motionDetected":true}}]`))
		}
	})

	srv := httptest.NewServer(mux)
	defer srv.Close()

	makeIngestor := func(apiKey string) *DataIngestor {
		return &DataIngestor{
			Config: config.Config{
				WeakAppURL:    srv.URL,
				WeakAppAPIKey: apiKey,
			},
			Client:   srv.Client(),
			Producer: &fakeProducer{},
		}
	}

	d401 := makeIngestor("401")
	_, err401, retry401 := d401.doRequest(context.Background())
	if err401 == nil {
		t.Fatalf("expected error for 401")
	}
	mustEqual(t, "401 retry", false, retry401)

	d403 := makeIngestor("403")
	_, err403, retry403 := d403.doRequest(context.Background())
	if err403 == nil {
		t.Fatalf("expected error for 403")
	}
	mustEqual(t, "403 retry", false, retry403)

	d404 := makeIngestor("404")
	_, err404, retry404 := d404.doRequest(context.Background())
	if err404 == nil {
		t.Fatalf("expected error for 404")
	}
	mustEqual(t, "404 retry", true, retry404)

	d429 := makeIngestor("429")
	_, err429, retry429 := d429.doRequest(context.Background())
	if err429 == nil {
		t.Fatalf("expected error for 429")
	}
	mustEqual(t, "429 retry", true, retry429)

	dok := makeIngestor("ok")
	data, errok, retryok := dok.doRequest(context.Background())
	if errok != nil {
		t.Fatalf("unexpected error for ok: %v", errok)
	}
	mustEqual(t, "ok retry", false, retryok)
	var items []models.ResponseItem
	if err := json.Unmarshal(data, &items); err != nil {
		t.Fatalf("failed to unmarshal ok payload: %v", err)
	}
	if len(items) != 1 {
		t.Fatalf("expected 1 item on success, got %d", len(items))
	}
}

type errTransport struct{}

func (e *errTransport) RoundTrip(req *http.Request) (*http.Response, error) {
	return nil, fmt.Errorf("simulated network error")
}

func TestDoRequest_ClientError(t *testing.T) {
	d := &DataIngestor{
		Config: config.Config{
			WeakAppURL:    "http://example.invalid",
			WeakAppAPIKey: "k",
		},
		Client:   &http.Client{Transport: &errTransport{}},
		Producer: &fakeProducer{},
	}

	_, err, retry := d.doRequest(context.Background())
	if err == nil {
		t.Fatalf("expected client error from doRequest when transport fails")
	}
	mustEqual(t, "client error retry", true, retry)
}

type readErrRC struct{}

func (r readErrRC) Read(p []byte) (int, error) { return 0, fmt.Errorf("simulated read error") }
func (r readErrRC) Close() error               { return nil }

type readErrTransport struct{}

func (r *readErrTransport) RoundTrip(req *http.Request) (*http.Response, error) {
	return &http.Response{
		StatusCode: http.StatusOK,
		Body:       readErrRC{},
		Header:     http.Header{"Content-Type": []string{"application/json"}},
		Request:    req,
	}, nil
}

func TestDoRequest_ReadAllError(t *testing.T) {
	d := &DataIngestor{
		Config: config.Config{
			WeakAppURL:    "http://example.invalid",
			WeakAppAPIKey: "k",
		},
		Client:   &http.Client{Transport: &readErrTransport{}},
		Producer: &fakeProducer{},
	}

	_, err, retry := d.doRequest(context.Background())
	if err == nil {
		t.Fatalf("expected read error from doRequest when reading body fails")
	}
	mustEqual(t, "read error retry", true, retry)
}

type badBody struct{}

func (b *badBody) Read(p []byte) (int, error) { return 0, fmt.Errorf("read error") }
func (b *badBody) Close() error               { return nil }

type roundTripperFunc func(*http.Request) (*http.Response, error)

func (f roundTripperFunc) RoundTrip(r *http.Request) (*http.Response, error) { return f(r) }

func TestDoRequest_ReadBodyError(t *testing.T) {
	d := &DataIngestor{
		Config: config.Config{
			WeakAppURL:    "http://example.invalid",
			WeakAppAPIKey: "k",
		},
		Client: &http.Client{Transport: roundTripperFunc(func(req *http.Request) (*http.Response, error) {
			return &http.Response{
				StatusCode: http.StatusOK,
				Body:       &badBody{},
				Header:     make(http.Header),
			}, nil
		})},
		Producer: &fakeProducer{},
	}

	_, err, retry := d.doRequest(context.Background())
	if err == nil {
		t.Fatalf("expected error when body Read fails")
	}
	mustEqual(t, "read error retry", true, retry)
}

func TestDoRequest_WhitespaceBody(t *testing.T) {
	h := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		_, _ = w.Write([]byte("   \n\t "))
	})
	srv := httptest.NewServer(h)
	defer srv.Close()

	d := &DataIngestor{
		Config: config.Config{
			WeakAppURL:    srv.URL,
			WeakAppAPIKey: "k",
		},
		Client:   srv.Client(),
		Producer: &fakeProducer{},
	}

	_, err, retry := d.doRequest(context.Background())
	if err == nil {
		t.Fatalf("expected validation error for whitespace body")
	}
	mustEqual(t, "whitespace retry", true, retry)
}

func TestHandleItem_UnsupportedType_NoSend(t *testing.T) {
	fp := &fakeProducer{}
	d := &DataIngestor{
		Config:   config.Config{KafkaTopic: "topic-x"},
		Producer: fp,
	}

	item := models.ResponseItem{Type: "unsupported_type", Name: "u1", Payload: json.RawMessage(`{"x":1}`)}
	d.handleItem(item)
	mustEqual(t, "unsupported send count", 0, fp.sendCount)
}

func TestPoll_UnmarshalError(t *testing.T) {
	h := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		// invalid JSON to trigger unmarshal error in poll
		_, _ = w.Write([]byte("{invalid"))
	})
	srv := httptest.NewServer(h)
	defer srv.Close()

	fp := &fakeProducer{}
	d := &DataIngestor{
		Config: config.Config{
			WeakAppURL:     srv.URL,
			WeakAppAPIKey:  "k",
			KafkaTopic:     "topic-x",
			InitialBackoff: 1 * time.Millisecond, // small backoff so retry loop advances quickly
			MaxBackoff:     5 * time.Millisecond,
		},
		Client:   srv.Client(),
		Producer: fp,
	}

	ctx, cancel := context.WithTimeout(context.Background(), 50*time.Millisecond)
	defer cancel()

	d.poll(ctx)
	mustEqual(t, "sendCount after unmarshal error", 0, fp.sendCount)
}

func TestDoRequest_ResponseTooLarge(t *testing.T) {
	h := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		b := make([]byte, 6*1024*1024)
		for i := range b {
			b[i] = 'x'
		}
		_, _ = w.Write(b)
	})

	srv := httptest.NewServer(h)
	defer srv.Close()

	d := &DataIngestor{
		Config: config.Config{
			WeakAppURL:    srv.URL,
			WeakAppAPIKey: "k",
		},
		Client:   srv.Client(),
		Producer: &fakeProducer{},
	}

	_, err, retry := d.doRequest(context.Background())
	if err == nil {
		t.Fatalf("expected error for too large response")
	}
	mustEqual(t, "too large retry", true, retry)
}

func TestDoRequest_BadRequest400(t *testing.T) {
	h := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		http.Error(w, "bad request", http.StatusBadRequest)
	})
	srv := httptest.NewServer(h)
	defer srv.Close()

	d := &DataIngestor{
		Config: config.Config{
			WeakAppURL:    srv.URL,
			WeakAppAPIKey: "k",
		},
		Client:   srv.Client(),
		Producer: &fakeProducer{},
	}

	_, err, retry := d.doRequest(context.Background())
	if err == nil {
		t.Fatalf("expected error for 400 Bad Request")
	}
	mustEqual(t, "400 retry", false, retry)
}

func TestDoRequest_RedirectStatus(t *testing.T) {
	h := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Location", "/other")
		w.WriteHeader(http.StatusFound)
	})
	srv := httptest.NewServer(h)
	defer srv.Close()

	d := &DataIngestor{
		Config: config.Config{
			WeakAppURL:    srv.URL,
			WeakAppAPIKey: "k",
		},
		Client:   srv.Client(),
		Producer: &fakeProducer{},
	}

	_, err, retry := d.doRequest(context.Background())
	if err == nil {
		t.Fatalf("expected error for redirect status")
	}

	mustEqual(t, "redirect retry", true, retry)
}
