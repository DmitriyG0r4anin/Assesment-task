# GraphQL API Gateway

`GraphQLAPIGateway` is a Rust service that exposes a single GraphQL API and fetches data from backend gRPC services (provided by `DataProcessor`).

It acts as a read gateway that:
- accepts GraphQL queries over HTTP,
- calls multiple gRPC endpoints behind the scenes,
- maps gRPC messages into GraphQL-friendly types,
- supports filtering, pagination, and cross-source aggregations.

## What This Service Does

- Serves GraphQL at:
  - `POST /graphql`
  - `GET /graphql` (GraphiQL UI)
  - `GET /graphiql` (GraphiQL UI)
- Connects to a gRPC backend via `GRPC_ENDPOINT`
- Exposes domain data:
  - air quality
  - energy
  - motion
  - rooms
- Exposes aggregation queries:
  - grouped by room
  - grouped by time buckets

This service is query-only right now (no GraphQL mutations/subscriptions).

---

## High-Level Architecture

1. Client sends GraphQL query to this gateway.
2. `src/graphql/schema.rs` resolves query fields.
3. Resolvers call `src/grpc_client.rs`.
4. `grpc_client` calls typed gRPC clients generated from `proto/parameters/parameters.proto`.
5. Results are transformed and returned as GraphQL response JSON.

Key files:
- `src/main.rs` - app startup, env loading, HTTP routes, server binding
- `src/graphql/schema.rs` - GraphQL query root, types, input filters, aggregation logic
- `src/graphql/schema.graphql` - SDL schema snapshot for API consumers
- `src/grpc_client.rs` - gRPC client wrapper + DTO mapping
- `build.rs` - compiles protobuf definitions and handles `protoc`

---

## Prerequisites

For local development:
- Rust toolchain (edition 2021 project)
- Cargo
- A reachable gRPC backend compatible with the `parameters` proto contract

Notes:
- Proto code generation happens at build time.
- `build.rs` can use an existing `protoc` in PATH and includes fallback logic to download/cache `protoc` when needed.

---

## Configuration

The service supports `.env` and environment variables.

`src/main.rs` loads `.env` on startup using `dotenvy`, then reads these variables:

- `HOST` (default: `localhost`)
- `PORT` (default: `4000`)
- `GRPC_ENDPOINT` (default: `http://localhost:8090`)

### Example `.env`

```env
HOST=localhost
PORT=4000
GRPC_ENDPOINT=http://localhost:8090
```

You can copy from `.env.example`:

```powershell
Copy-Item .env.example .env
```

---

## Run Locally

From `GraphQLAPIGateway/`:

```powershell
cargo build
cargo run
```

Expected startup logs:
- GraphQL API Gateway running on `http://<HOST>:<PORT>`
- routes:
  - `POST /graphql`
  - `GET /graphiql`

If `GRPC_ENDPOINT` is wrong/unreachable, startup fails with a gRPC connection error.

---

## Run with Docker Compose (Full Stack)

From repo root:

```powershell
docker compose up --build
```

In this repository's compose config:
- gateway container name: `graphql-api-gateway`
- published port: `4000:4000`
- gateway `GRPC_ENDPOINT` is set to `http://dataprocessor:8090`

Then open:
- GraphiQL: [http://localhost:4000/graphiql](http://localhost:4000/graphiql)

---

## GraphQL Schema

The gateway schema is defined in Rust (`src/graphql/schema.rs`) and mirrored as SDL in `src/graphql/schema.graphql`.

Main root query fields:
- `airQualities`
- `airQuality`
- `energies`
- `energy`
- `motions`
- `motion`
- `rooms`
- `room`
- `aggregateByRoom`
- `aggregateByTime`

Inputs:
- `SensorFilter`
- `MotionFilter`
- `RoomFilter`
- `PaginationInput`

Output connection types:
- `AirQualityConnection`
- `EnergyConnection`
- `MotionConnection`
- `RoomConnection`

Aggregations:
- `RoomAggregation`
- `TimeAggregation`

Custom scalar:
- `DateTime`

---

## Example Queries

### 1) Paginated air quality data

```graphql
query {
  airQualities(
    filter: { roomId: "room-1" }
    pagination: { offset: 0, limit: 10 }
  ) {
    items {
      id
      roomId
      timestamp
      co2
      pm25
      humidity
    }
    totalCount
    hasNextPage
  }
}
```

### 2) Motion events with client-side `isDetected` filter

```graphql
query {
  motions(
    filter: {
      roomId: "room-1"
      isDetected: true
    }
    pagination: { limit: 20 }
  ) {
    items {
      id
      roomId
      timestamp
      isDetected
    }
    totalCount
    hasNextPage
  }
}
```

### 3) Aggregation by room

```graphql
query {
  aggregateByRoom(startTime: "2025-01-01T00:00:00Z", endTime: "2025-01-02T00:00:00Z") {
    roomId
    roomName
    avgCo2
    avgPm25
    avgHumidity
    avgEnergy
    motionCount
    totalCount
  }
}
```

### 4) Aggregation by time bucket

```graphql
query {
  aggregateByTime(
    roomId: "room-1"
    startTime: "2025-01-01T00:00:00Z"
    endTime: "2025-01-01T12:00:00Z"
    intervalMinutes: 30
  ) {
    timestamp
    avgCo2
    avgEnergy
    motionCount
    totalCount
  }
}
```

---

## Behavior and Implementation Notes

- Pagination defaults:
  - `offset = 0`
  - `limit = 20`
  - negative `offset` is clamped to `0`
  - `limit` is clamped to minimum `1`
- `motions(filter.isDetected: ...)` is currently applied in gateway logic (client-side), not by the gRPC request itself.
- Aggregation queries fetch multiple gRPC sources concurrently (`tokio::join!`).
- CORS is currently permissive (`allow any origin/method/header`) for development convenience.

---

## Troubleshooting

### Service fails at startup with gRPC connection error
- Check `GRPC_ENDPOINT`.
- Confirm backend service is running and reachable from where the gateway runs.
- In compose mode, use `http://dataprocessor:8090`.

### `cargo build` fails around proto generation
- Ensure `protoc` is available (or let `build.rs` fallback mechanism fetch/cache it).
- Re-run build:
  ```powershell
  cargo clean
  cargo build
  ```

### GraphiQL opens but queries return GraphQL errors
- Backend may be reachable but returning domain errors.
- Inspect gateway logs and verify gRPC backend has data and healthy dependencies (Kafka, MongoDB, etc. in full stack mode).

---

## Quick Start Checklist

1. Copy `.env.example` to `.env`.
2. Set `GRPC_ENDPOINT` to your backend.
3. Run `cargo run` in `GraphQLAPIGateway/`.
4. Open [http://localhost:4000/graphiql](http://localhost:4000/graphiql).
5. Execute one of the example queries above.
