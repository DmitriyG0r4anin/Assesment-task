package config

import (
	"time"
)

const (
	EnvWeakAppURL      = "WEAKAPP_URL"
	EnvKafkaBrokers    = "KAFKA_BROKERS"
	EnvKafkaTopic      = "KAFKA_TOPIC"
	EnvPollInterval    = "POLL_INTERVAL"
	EnvInitialBackoff  = "INITIAL_BACKOFF"
	EnvMaxBackoff      = "MAX_BACKOFF"
	EnvRequestTimeout  = "REQUEST_TIMEOUT"
	EnvWeakAppAPIKey   = "WEAKAPP_API_KEY"
)

type Config struct {
	WeakAppURL     string        // Base URL of the WeakApp API (e.g. http://weak-app:8080)
	WeakAppAPIKey  string        // API key used when calling WeakApp
	KafkaBrokers   []string      // List of Kafka broker addresses (comma-separated in env, parsed into slice)
	KafkaTopic     string        // Kafka topic to publish messages to
	PollInterval   time.Duration // Interval between polls (Go duration string, e.g. "5m")
	InitialBackoff time.Duration // Initial backoff duration for retries
	MaxBackoff     time.Duration // Maximum backoff duration for retries
	RequestTimeout time.Duration // HTTP request timeout for calls to WeakApp
}
