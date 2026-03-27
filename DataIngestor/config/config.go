package config

import (
	"time"
)

const (
	EnvWeakAppURL     = "WEAKAPP_URL"
	EnvKafkaBrokers   = "KAFKA_BROKERS"
	EnvKafkaTopic     = "KAFKA_TOPIC"
	EnvPollInterval   = "POLL_INTERVAL"
	EnvInitialBackoff = "INITIAL_BACKOFF"
	EnvMaxBackoff     = "MAX_BACKOFF"
	EnvRequestTimeout = "REQUEST_TIMEOUT"
	EnvWeakAppAPIKey  = "WEAKAPP_API_KEY"
	EnvProducerRetryCount = "PRODUCER_RETRY_COUNT"
)

type Config struct {
	WeakAppURL     string
	WeakAppAPIKey  string
	KafkaBrokers   []string
	KafkaTopic     string
	PollInterval   time.Duration
	InitialBackoff time.Duration
	MaxBackoff     time.Duration
	RequestTimeout time.Duration
	ProducerRetryCount int
}
