# DataProcessor

A .NET service that consumes meter data from Kafka, persists it in MongoDB, and exposes read APIs over gRPC. Detected motion events are forwarded to a separate Kafka topic for real-time notifications.

## Overview

DataProcessor sits at the center of the ingestion pipeline:

- Consumes messages from the `meter-data` Kafka topic (produced by DataIngestor)
- Creates rooms on first sight and stores typed metric records in MongoDB
- Publishes motion detection events to the `motion-events` Kafka topic
- Serves query endpoints via gRPC for the GraphQL API Gateway

Supported metric types: **air quality**, **energy**, and **motion**.

## Architecture

```
Kafka (meter-data) ──▶ KafkaConsumerService ──▶ MongoDB
                              │
                              │ motion detected
                              ▼
                       Kafka (motion-events)

gRPC clients ◀── AirQuality / Energy / Motion / Room services ◀── MongoDB repositories
```

| Layer | Project | Responsibility |
|-------|---------|----------------|
| Domain | `DataProcessor.Domain` | Entities, constants, shared types |
| Application | `DataProcessor.Application` | Queries, models, repository abstractions |
| Infrastructure | `DataProcessor.Infrastructure` | MongoDB, Kafka consumer/producer |
| Presentation | `DataProcessor.Presentation` | gRPC services, host startup |

## gRPC Services

Defined in `DataProcessor.Presentation/Protos/parameters.proto`:

- `AirQualityService` — list and get air quality readings (CO₂, PM2.5, humidity)
- `EnergyService` — list and get energy consumption records
- `MotionService` — list and get motion events
- `RoomService` — list and get rooms

Kestrel is configured for HTTP/2 (required for gRPC).

## Configuration

Settings are in `DataProcessor.Presentation/appsettings.json` and can be overridden via environment variables or user secrets.

| Setting | Description | Default (Docker Compose) |
|---------|-------------|--------------------------|
| `MongoDb:ConnectionString` | MongoDB connection string | `mongodb://mongodb:27017` |
| `MongoDb:DatabaseName` | Database name | `data_metrics` |
| `Kafka:Brokers` | Kafka bootstrap servers | `broker:29092` |
| `Kafka:MetricTopic` | Topic for incoming meter data | `meter-data` |
| `Kafka:MotionTopic` | Topic for motion notifications | `motion-events` |
| `Kafka:GroupId` | Consumer group id | `data-processor-group` |

## Running

### With Docker Compose

From the repository root:

```bash
docker compose up data-processor
```

Published ports: **8090** (HTTP/gRPC), **8091** (HTTPS).

### Local development

```bash
cd DataProcessor
dotnet run --project DataProcessor.Presentation
```

Ensure MongoDB and Kafka are reachable and update `appsettings.Development.json` accordingly.

### Tests

```bash
dotnet test DataProcessor.slnx
```

## Dependencies

- MongoDB — persistent storage for rooms and metrics
- Apache Kafka — inbound meter data and outbound motion events
- Confluent.Kafka — .NET Kafka client
