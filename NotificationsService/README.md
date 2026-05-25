# NotificationsService

A lightweight .NET service that bridges Kafka motion events to browser clients over SignalR.

## Overview

When DataProcessor detects motion, it publishes a message to the `motion-events` Kafka topic. NotificationsService consumes those messages and broadcasts them to all connected web clients through a SignalR hub.

This decouples real-time push delivery from the GraphQL read path used for historical data.

## Architecture

```
DataProcessor ──▶ Kafka (motion-events) ──▶ KafkaMotionConsumer ──▶ MotionHub (SignalR) ──▶ frontend
```

Clients receive events on the `MotionDetected` method with `{ roomName, isDetected, timestamp }`.

## Configuration

Settings in `appsettings.json` (overridable via environment variables):

| Setting | Description | Default (Docker Compose) |
|---------|-------------|--------------------------|
| `Kafka:Brokers` | Kafka bootstrap servers | `broker:29092` |
| `Kafka:MotionTopic` | Topic to consume | `motion-events` |
| `Kafka:GroupId` | Consumer group id | `notifications-group` |

Docker Compose also maps `Kafka_Brokers` and `Kafka_MotionTopic` environment variables.

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Health check (returns `OK`) |
| WebSocket | `/notifications/motionHub` | SignalR hub for motion events |

CORS is configured to allow any origin with credentials for development.

## Running

### With Docker Compose

From the repository root:

```bash
docker compose up notifications-service
```

Published port: **8092**.

The frontend container proxies WebSocket traffic to this service at `/notifications/motionHub`.

### Local development

```bash
cd NotificationsService
dotnet run
```

Ensure Kafka is running and the `motion-events` topic receives messages from DataProcessor.

## Dependencies

- Apache Kafka — source of motion events
- Confluent.Kafka — .NET Kafka client
- ASP.NET Core SignalR — real-time push to browsers
