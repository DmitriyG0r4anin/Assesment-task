package ingestor

import (
	"context"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
	"strings"
	"time"

	"github.com/dataingestor/broker"
	"github.com/dataingestor/config"
	"github.com/dataingestor/models"
)

type DataIngestor struct {
	Config   config.Config
	Client   *http.Client
	Producer broker.Producer
}

func NewDataIngestor(config config.Config) (*DataIngestor, error) {
	client := &http.Client{
		Timeout: config.RequestTimeout,
	}

	kafkaConfig := broker.SaramaConfig(config.ProducerRetryCount)
	producer, err := broker.NewKafkaProducer(config.KafkaBrokers, config.KafkaTopic, kafkaConfig)
	if err != nil {
		return nil, fmt.Errorf("failed to create Kafka producer: %w", err)
	}

	return &DataIngestor{
		Config:   config,
		Client:   client,
		Producer: producer,
	}, nil
}

func (d *DataIngestor) Close() error {
	if d.Producer != nil {
		return d.Producer.Close()
	}
	return nil
}

func (d *DataIngestor) fetchDataFromWeakApp(ctx context.Context) ([]byte, error) {
	backoff := d.Config.InitialBackoff

	for attempt := 0; ; attempt++ {
		if attempt > 0 {
			log.Printf("Retry attempt %d/ after %v", attempt, backoff)

			select {
			case <-ctx.Done():
				return nil, ctx.Err()
			case <-time.After(backoff):
			}

			// Exponential backoff with cap
			backoff = min(backoff*2, d.Config.MaxBackoff)
		}

		data, err, shouldRetry := d.doRequest(ctx)
		if err == nil {
			return data, nil
		}

		if !shouldRetry {
			return nil, err
		}
	}
}

func (d *DataIngestor) doRequest(ctx context.Context) ([]byte, error, bool) {
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, d.Config.WeakAppURL+"/meters", nil)
	if err != nil {
		return nil, fmt.Errorf("failed to create request: %w", err), false
	}

	req.Header.Set("X-Api-Key", d.Config.WeakAppAPIKey)
	req.Header.Set("Accept", "application/json")

	resp, err := d.Client.Do(req)
	if err != nil {
		log.Printf("Request failed: %v", err)
		return nil, err, true
	}
	defer resp.Body.Close()

	// Protect against extremely large responses from upstream to avoid OOM.
	const maxResponseBytes = 5 * 1024 * 1024 // 5 MB
	lr := io.LimitReader(resp.Body, maxResponseBytes+1)
	body, err := io.ReadAll(lr)
	if err != nil {
		return nil, fmt.Errorf("failed to read response body: %w", err), true
	}
	if int64(len(body)) > maxResponseBytes {
		return nil, fmt.Errorf("response too large (> %d bytes)", maxResponseBytes), true
	}

	switch resp.StatusCode {
	case http.StatusOK:
		log.Printf("Successfully fetched data from WeakApp")
		if validated, err, retry := validateResponse(body); err != nil {
			if retry {
				log.Printf("Response validation error (transient): %v", err)
				return nil, err, true
			}
			log.Printf("Response validation error: %v", err)
			return nil, err, false
		} else {
			return validated, nil, false
		}

	case http.StatusBadRequest:
		log.Printf("Bad request (400): %s", string(body))
		return nil, fmt.Errorf("bad request: %s", string(body)), false

	case http.StatusUnauthorized:
		log.Printf("Unauthorized (401): check API key")
		return nil, fmt.Errorf("unauthorized: invalid API key"), false

	case http.StatusForbidden:
		log.Printf("Forbidden (403): access denied")
		return nil, fmt.Errorf("forbidden: access denied"), false

	case http.StatusNotFound:
		// 404 - Not found, might be temporary, retry with backoff
		log.Printf("Not found (404): resource not available")
		return nil, fmt.Errorf("resource not found"), true

	case http.StatusTooManyRequests:
		log.Printf("Rate limited (429): too many requests")
		return nil, fmt.Errorf("rate limited"), true

	case http.StatusInternalServerError, http.StatusBadGateway, http.StatusServiceUnavailable, http.StatusGatewayTimeout:
		log.Printf("Server error (%d): %s", resp.StatusCode, string(body))
		return nil, fmt.Errorf("server error: %d", resp.StatusCode), true

	default:
		log.Printf("Unexpected status code (%d): %s", resp.StatusCode, string(body))
		if resp.StatusCode >= 400 && resp.StatusCode < 500 {
			return nil, fmt.Errorf("client error: %d", resp.StatusCode), false
		}
		return nil, fmt.Errorf("unexpected error: %d", resp.StatusCode), true
	}
}

func validateResponse(body []byte) ([]byte, error, bool) {
	if len(body) == 0 {
		return nil, fmt.Errorf("empty response body"), true
	}

	trimmed := strings.TrimSpace(string(body))
	if trimmed == "" {
		return nil, fmt.Errorf("empty response body"), true
	}

	if trimmed == "[]" || trimmed == "null" {
		return nil, fmt.Errorf("empty or null response: %s", trimmed), true
	}

	// Try to parse JSON
	var parsed any
	if err := json.Unmarshal(body, &parsed); err != nil {
		return nil, fmt.Errorf("malformed JSON response: %w", err), true
	}

	if m, ok := parsed.(map[string]any); ok {
		if errVal, exists := m["error"]; exists {
			return nil, fmt.Errorf("upstream reported error: %v", errVal), true
		}
	}

	return body, nil, false
}

func (d *DataIngestor) sendToKafka(data []byte) error {
	partition, offset, err := d.Producer.Send(&broker.Message{
		Value: data,
	})
	if err != nil {
		return fmt.Errorf("failed to send message to Kafka: %w", err)
	}
	log.Printf("Message sent to Kafka: partition=%d, offset=%d", partition, offset)
	return nil
}

func (d *DataIngestor) Run(ctx context.Context) error {
	log.Printf("Starting DataIngestor - polling every %v", d.Config.PollInterval)

	ticker := time.NewTicker(d.Config.PollInterval)
	defer ticker.Stop()

	d.poll(ctx)

	for {
		select {
		case <-ctx.Done():
			log.Println("Shutting down DataIngestor...")
			return ctx.Err()
		case <-ticker.C:
			d.poll(ctx)
		}
	}
}

func (d *DataIngestor) poll(ctx context.Context) {
	log.Println("Polling WeakApp for data...")

	data, err := d.fetchDataFromWeakApp(ctx)
	if err != nil {
		log.Printf("Failed to fetch data from WeakApp: %v", err)
		return
	}

	var items []models.ResponseItem
	if err := json.Unmarshal(data, &items); err != nil {
		log.Printf("Failed to unmarshal WeakApp response into items: %v; raw: %s", err, string(data))
		return
	}

	for _, item := range items {
		d.handleItem(item)
	}

	log.Println("Successfully processed and forwarded items")
}

func (d *DataIngestor) handleItem(item models.ResponseItem) {
	// local helper to marshal an outgoing message and publish to Kafka
	publish := func(typ, name string, payload any) {
		out := models.OutgoingMessage{
			Type:      typ,
			Name:      name,
			Payload:   payload,
			Timestamp: time.Now().UTC(),
		}
		b, err := json.Marshal(out)
		if err != nil {
			log.Printf("Failed to marshal outgoing %s message for '%s': %v", typ, name, err)
			return
		}
		if err := d.sendToKafka(b); err != nil {
			log.Printf("Failed to send %s message to Kafka for '%s': %v", typ, name, err)
		}
	}

	switch item.Type {
	case "air_quality":
		var p models.AirQualityPayload
		if err := json.Unmarshal(item.Payload, &p); err != nil {
			log.Printf("Failed to unmarshal air_quality payload for '%s': %v", item.Name, err)
			return
		}
		publish("air_quality", item.Name, p)

	case "motion":
		var p models.MotionPayload
		if err := json.Unmarshal(item.Payload, &p); err != nil {
			log.Printf("Failed to unmarshal motion payload for '%s': %v", item.Name, err)
			return
		}
		publish("motion", item.Name, p)

	case "energy":
		var p models.EnergyPayload
		if err := json.Unmarshal(item.Payload, &p); err != nil {
			log.Printf("Failed to unmarshal energy payload for '%s': %v", item.Name, err)
			return
		}
		publish("energy", item.Name, p)

	default:
		log.Printf("Skipping unsupported item type '%s' for '%s'", item.Type, item.Name)
	}
}
