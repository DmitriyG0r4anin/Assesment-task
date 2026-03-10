package models

import (
	"time"
)

type Config struct {
	WeakAppURL     string        // Base URL of the WeakApp API (e.g. http://weak-app:8080)
	WeakAppAPIKey  string        // API key used when calling WeakApp
	KafkaBrokers   []string      // List of Kafka broker addresses (comma-separated in env, parsed into slice)
	KafkaTopic     string        // Kafka topic to publish messages to
	PollInterval   time.Duration // Interval between polls (Go duration string, e.g. "5m")
	MaxRetries     int           // Number of retry attempts for transient errors
	InitialBackoff time.Duration // Initial backoff duration for retries
	MaxBackoff     time.Duration // Maximum backoff duration for retries
	RequestTimeout time.Duration // HTTP request timeout for calls to WeakApp
}
