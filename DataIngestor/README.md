# DataIngestor

A resilient Go application that periodically fetches data from the WeakApp API and forwards it to the DataProcessor service via Apache Kafka.

## Overview

DataIngestor is responsible for:
- Polling the WeakApp API every 5 minutes (configurable)
- Handling various HTTP error responses with appropriate retry logic
- Sending successfully retrieved data to Kafka for downstream processing

## Features

### Resilience Logic

The application implements robust error handling for various HTTP status codes:

| Status Code | Behavior |
|-------------|----------|
| 200 OK | Success - validate data for correctness and forward to Kafka |
| 400 Bad Request | No retry - client error |
| 401 Unauthorized | No retry - API key issue |
| 403 Forbidden | No retry - access denied |
| 404 Not Found | Retry with backoff |
| 429 Too Many Requests | Retry with backoff |
| 5xx Server Errors | Retry with backoff |

### Retry Strategy

- **Max Retries**: 5 attempts
- **Initial Backoff**: 1 second
- **Max Backoff**: 30 seconds
- **Backoff Type**: Exponential

### Graceful Shutdown

The application handles SIGINT and SIGTERM signals for clean shutdown, ensuring:
- In-flight requests complete
- Kafka producer flushes pending messages
- Resources are properly released

## Configuration

The application is configured via environment variables and an optional `.env` file for sensitive values.

Use the included `.env.example` as a template. Copy it to `.env` and fill in sensitive values (do not commit `.env` to version control).

| Variable | Description | Default / Notes |
| `WEAKAPP_URL` | Base URL of WeakApp API | `http://weak-app:8080` |
| `WEAKAPP_API_KEY` | API key for WeakApp authentication | SENSITIVE — provide via `.env` or environment |
| `KAFKA_BROKERS` | Comma-separated list of Kafka brokers | SENSITIVE — provide via `.env` or environment |
| `KAFKA_TOPIC` | Kafka topic for meter data | `meter-data` |
| `POLL_INTERVAL` | Interval between API polls | `5m` |

## Building

### Local Build

```bash
go build -o dataingestor .
```

### Docker Build

```bash
docker build -t dataingestor .
```

## Running

### With Docker Compose

From the project root:

```bash
docker-compose up data-ingestor
```

### Standalone (for development)

For local development you can use the `.env.example` to create a `.env` file and load it. DO NOT commit your real `.env` with secrets.

```bash
# copy the example and edit the values (set WEAKAPP_API_KEY, KAFKA_BROKERS, etc.)
cp .env.example .env

# load environment variables from .env (Linux/macOS example)
export $(grep -v '^#' .env | xargs)

# run the application
go run .
```

Alternatively you may export variables individually, but prefer using `.env` for sensitive values.

## Data Format

The data sent to Kafka is wrapped with metadata:

```json
{
  "data": { /* raw data from WeakApp /meters endpoint */ },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Dependencies

- [IBM/sarama](https://github.com/IBM/sarama) - Kafka client library for Go

## Architecture

```
┌─────────────┐     HTTP GET      ┌──────────┐
│ DataIngestor│ ─────────────────▶│ WeakApp  │
│             │ ◀─────────────────│   API    │
└─────────────┘    JSON Response  └──────────┘
       │
       │ Kafka Message
       ▼
┌─────────────┐
│   Kafka     │
│   Broker    │
└─────────────┘
       │
       │ Consumer
       ▼
┌─────────────┐
│   Data      │
│  Processor  │
└─────────────┘
```
