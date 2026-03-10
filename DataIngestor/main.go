package main

import (
	"context"
	"log"
	"os"
	"os/signal"
	"strings"
	"syscall"
	"time"

	"github.com/dataingestor/ingestor"
	"github.com/dataingestor/models"
)

func loadDotEnv(path string) {
	data, err := os.ReadFile(path)
	if err != nil {
		return
	}
	lines := strings.SplitSeq(string(data), "\n")
	for line := range lines {
		line = strings.TrimSpace(line)
		if line == "" || strings.HasPrefix(line, "#") {
			continue
		}
		parts := strings.SplitN(line, "=", 2)
		if len(parts) != 2 {
			continue
		}
		key := strings.TrimSpace(parts[0])
		val := strings.TrimSpace(parts[1])

		val = strings.Trim(val, `"'`)
		if key == "" {
			continue
		}
		if _, exists := os.LookupEnv(key); !exists {
			_ = os.Setenv(key, val)
		}
	}
}

func getEnv(key, defaultValue string) string {
	loadDotEnv(".env")
	if v := os.Getenv(key); v != "" {
		return v
	}
	return defaultValue
}

func getEnvDuration(key string, defaultValue time.Duration) time.Duration {
	if value := os.Getenv(key); value != "" {
		if d, err := time.ParseDuration(value); err == nil {
			return d
		}
	}
	return defaultValue
}

func main() {
	log.SetFlags(log.LstdFlags | log.Lmicroseconds)
	log.Println("DataIngestor starting (thin main)...")

	weakAppURL := getEnv("WEAKAPP_URL", "http://weak-app:8080")
	weakAppAPIKey := getEnv("WEAKAPP_API_KEY", "")
	kafkaBrokersEnv := getEnv("KAFKA_BROKERS", "")
	kafkaTopic := getEnv("KAFKA_TOPIC", "meter-data")

	kafkaBrokers := []string{}
	if kafkaBrokersEnv != "" {
		for b := range strings.SplitSeq(kafkaBrokersEnv, ",") {
			if s := strings.TrimSpace(b); s != "" {
				kafkaBrokers = append(kafkaBrokers, s)
			}
		}
	}

	if weakAppAPIKey == "" {
		log.Fatal("WEAKAPP_API_KEY is not set. Provide it via environment or a .env file.")
	}
	if len(kafkaBrokers) == 0 {
		log.Fatal("KAFKA_BROKERS is not set or empty. Provide comma-separated brokers via environment or a .env file.")
	}

	cfg := models.Config{
		WeakAppURL:     weakAppURL,
		WeakAppAPIKey:  weakAppAPIKey,
		KafkaBrokers:   kafkaBrokers,
		KafkaTopic:     kafkaTopic,
		PollInterval:   getEnvDuration("POLL_INTERVAL", 5*time.Minute),
		MaxRetries:     5,
		InitialBackoff: 1 * time.Second,
		MaxBackoff:     30 * time.Second,
		RequestTimeout: 30 * time.Second,
	}

	ing, err := ingestor.NewDataIngestor(cfg)
	if err != nil {
		log.Fatalf("failed to create DataIngestor: %v", err)
	}

	defer func() {
		if err := ing.Close(); err != nil {
			log.Printf("error closing ingestor: %v", err)
		}
	}()

	ctx, cancel := context.WithCancel(context.Background())
	sigChan := make(chan os.Signal, 1)
	signal.Notify(sigChan, syscall.SIGINT, syscall.SIGTERM)
	go func() {
		sig := <-sigChan
		log.Printf("received signal %v, shutting down...", sig)
		cancel()
	}()

	if err := ing.Run(ctx); err != nil && err != context.Canceled {
		log.Fatalf("DataIngestor.Run returned error: %v", err)
	}

	log.Println("DataIngestor shutdown complete")
}
