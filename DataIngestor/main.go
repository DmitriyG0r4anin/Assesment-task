package main

import (
	"context"
	"log"
	"os"
	"os/signal"
	"strconv"
	"strings"
	"syscall"
	"time"

	"github.com/dataingestor/config"
	"github.com/dataingestor/ingestor"
)

func loadDotEnv(path string) {
	data, err := os.ReadFile(path)
	if err != nil {
		return
	}
	lines := strings.Split(string(data), "\n")
	for _, line := range lines {
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

	weakAppURL := getEnv(config.EnvWeakAppURL, "http://weak-app:8080")

	weakAppAPIKey := getEnv(config.EnvWeakAppAPIKey, "")
	if weakAppAPIKey == "" {
		log.Fatal("WEAKAPP_API_KEY is not set. Provide it via environment or a .env file.")
	}

	kafkaBrokersEnv := getEnv(config.EnvKafkaBrokers, "")
	kafkaTopic := getEnv(config.EnvKafkaTopic, "meter-data")
	pollInterval := getEnvDuration(config.EnvPollInterval, 5*time.Minute)
	initialBackoff := getEnvDuration(config.EnvInitialBackoff, 1*time.Minute)
	maxBackoff := getEnvDuration(config.EnvMaxBackoff, 30*time.Second)
	requestTimeout := getEnvDuration(config.EnvRequestTimeout, 5*time.Minute)
	producerRetryCount, err :=  strconv.Atoi(getEnv(config.EnvProducerRetryCount, "5"))
	if err != nil {
		log.Fatal("PRODUCER_RETRY_COUNT is not a number. Provide an integer value for this parameter")

	if kafkaBrokersEnv != "" {
		for _, b := range strings.Split(kafkaBrokersEnv, ",") {
			if s := strings.TrimSpace(b); s != "" {
				kafkaBrokers = append(kafkaBrokers, s)
			}
		}
	}

	if len(kafkaBrokers) == 0 {
		log.Fatal("KAFKA_BROKERS is not set or empty. Provide comma-separated brokers via environment or a .env file.")
	}

	cfg := config.Config{
		WeakAppURL:     weakAppURL,
		WeakAppAPIKey:  weakAppAPIKey,
		KafkaBrokers:   kafkaBrokers,
		KafkaTopic:     kafkaTopic,
		PollInterval:   pollInterval,
		InitialBackoff: initialBackoff,
		MaxBackoff:     maxBackoff,
		RequestTimeout: requestTimeout,
		ProducerRetryCount: producerRetryCount,
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

	ctx, stop := signal.NotifyContext(context.Background(), syscall.SIGINT, syscall.SIGTERM)
	defer stop()

	if err := ing.Run(ctx); err != nil && err != context.Canceled {
		log.Fatalf("DataIngestor.Run returned error: %v", err)
	}

	log.Println("DataIngestor shutdown complete")
}
