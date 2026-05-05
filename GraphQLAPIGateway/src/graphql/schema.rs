// ============================================================================
// schema.rs — GraphQL schema: types, input objects, and query resolvers
// ============================================================================
//
// This file defines the entire public GraphQL API of the gateway.  It is
// built with the `async-graphql` crate, which is the Rust equivalent of
// Hot Chocolate or GraphQL.NET in the .NET world.
//
// .NET parallel overview
// ──────────────────────
// async-graphql concept       │ .NET / Hot Chocolate equivalent
// ────────────────────────────┼──────────────────────────────────────────────
// #[derive(SimpleObject)]     │ [GraphQLType] / ObjectType attribute
// #[derive(InputObject)]      │ [GraphQLType] / InputType attribute
// #[Object] impl QueryRoot    │ QueryType class with resolver methods
// ctx.data::<T>()?            │ service resolved from DI container
// async_graphql::Result<T>    │ Task<T> (errors surface as GraphQL errors)
// Option<T>                   │ T? (nullable GraphQL field)
//
// Module layout
// ─────────────
// 1. Input types   — filter + pagination structs used as query arguments
// 2. Object types  — output structs returned to GraphQL clients
// 3. Helper fns    — filtering, pagination, aggregation math
// 4. QueryRoot     — resolver methods (one per GraphQL query field)
//
// Data flow inside every resolver
// ────────────────────────────────
// 1. Extract `GrpcClient` from the GraphQL context (DI-style lookup).
// 2. Call one or more `GrpcClient` methods (each performs a gRPC RPC).
// 3. Optionally apply extra client-side filtering or aggregation.
// 4. Return the result as a GraphQL type.
//
// Additional notes for .NET developers — Rust-specific behavior you'll see
// in the codebase:
// - Ownership & cloning: Rust enforces ownership at compile time. When the
//   code needs to share data (e.g. the gRPC `GrpcClient` inside the GraphQL
//   `Schema`) it requires types to be `Clone` and often `Send + Sync`. This
//   is why `GrpcClient` derives `Clone` — cloning is cheap because it clones
//   the underlying `Channel` (similar to copying a reference to a shared
//   HttpClient).
// - Concurrency model: `tokio` is the async runtime (equivalent to the
//   thread-pool + async/await infrastructure you know in .NET). `tokio::join!`
//   runs futures concurrently (like `Task.WhenAll`) without spawning OS
//   threads for each task.
// - Error handling: functions and resolvers return `Result<T, E>` (here
//   `async_graphql::Result<T>`). The `?` operator propagates errors; async-
//   graphql converts propagated errors into GraphQL error entries in the
//   response (roughly analogous to exceptions bubbling out of resolvers).
// - Option vs nullable: `Option<T>` maps to nullable GraphQL fields (T? in C#).
// - Protobuf conversions: generated proto structs are converted into local
//   DTO structs (e.g. `AirQualityDto`) to decouple the rest of the app from
//   the generated code — similar to mapping gRPC generated messages to PCLs
//   or domain DTOs in .NET.
// - Blocking vs async: avoid blocking calls in async functions. All IO here
//   (gRPC) is async/await-based and uses the `tokio` runtime, just as you'd
//   prefer async gRPC in .NET.
// - Thread-safety: types stored in the GraphQL schema or used across await
//   points must satisfy thread-safety bounds (`Send + Sync`) — you'll notice
//   those constraints indirectly via trait bounds on APIs.
//
// These notes are intended to help you map Rust idioms to the .NET mental
// model while reading the rest of this file.
// ============================================================================

use std::collections::{BTreeMap, HashMap};

use async_graphql::{Context, InputObject, Object, SimpleObject};
use chrono::{DateTime, Utc};

use crate::grpc_client::{AirQualityDto, EnergyDto, GrpcClient, MotionDto, RoomDto};

/// Maps a gRPC error to a GraphQL error and records structured context for logs.
#[inline]
fn gql_grpc_err(context: &'static str, e: impl std::fmt::Display) -> async_graphql::Error {
    tracing::error!(context, error = %e, "gRPC call failed");
    async_graphql::Error::new(e.to_string())
}

// ============================================================================
// 1. Input types
// ============================================================================

/// Common time-range + room filter shared by AirQuality, Energy, and Motion
/// queries.  Every field is optional; omitting them returns all records.
///
/// .NET parallel: a query-parameter model class used for filtering, e.g.
///   public class SensorFilter {
///       public DateTime? StartTime { get; set; }
///       public DateTime? EndTime   { get; set; }
///       public string?   RoomId    { get; set; }
///   }
#[derive(Debug, Default, InputObject)]
pub struct SensorFilter {
    /// Only return readings at or after this timestamp (inclusive).
    pub timestamp_start: Option<DateTime<Utc>>,
    /// Only return readings at or before this timestamp (inclusive).
    pub timestamp_end: Option<DateTime<Utc>>,
    /// Only return readings from this room.
    pub room_id: Option<String>,
}

/// Filter for the `motions` query — extends the common sensor filter with a
/// presence flag so callers can request only "detected" or "not detected"
/// events.
#[derive(Debug, Default, InputObject)]
pub struct MotionFilter {
    pub timestamp_start: Option<DateTime<Utc>>,
    pub timestamp_end: Option<DateTime<Utc>>,
    pub room_id: Option<String>,
    /// When set, only returns events where `is_detected` matches this value.
    pub is_detected: Option<bool>,
}

/// Filter for the `rooms` query.
/// Note: the proto's `GetRoomsRequest` does not accept a `room_id` filter —
/// use the `room(id: …)` query to fetch a specific room by ID instead.
#[derive(Debug, Default, InputObject)]
pub struct RoomFilter {
    /// Only return rooms that had activity at or after this timestamp.
    pub timestamp_start: Option<DateTime<Utc>>,
    /// Only return rooms that had activity at or before this timestamp.
    pub timestamp_end: Option<DateTime<Utc>>,
}

/// Pagination arguments accepted by every list query.
///
/// .NET parallel:
///   public record PaginationInput(int Offset = 0, int Limit = 20);
#[derive(Debug, Default, InputObject)]
pub struct PaginationInput {
    /// Number of items to skip (default: 0).
    pub offset: Option<i32>,
    /// Maximum number of items to return (default: 20).
    pub limit: Option<i32>,
}

// ============================================================================
// 2. GraphQL object types
// ============================================================================

// ── Air Quality ──────────────────────────────────────────────────────────────

/// A single air-quality sensor reading.
///
/// `#[derive(SimpleObject)]` tells async-graphql to expose every public field
/// as a GraphQL field automatically — similar to `[GraphQLType]` on a POCO in
/// Hot Chocolate.
#[derive(Debug, Clone, SimpleObject)]
pub struct AirQuality {
    pub id: String,
    pub room_id: String,
    pub timestamp: DateTime<Utc>,
    /// Fine-particulate-matter concentration.
    pub pm25: i32,
    /// CO₂ concentration in ppm.
    pub co2: i32,
    /// Relative humidity in %.
    pub humidity: i32,
}

/// Paginated list of `AirQuality` readings.
#[derive(Debug, SimpleObject)]
pub struct AirQualityConnection {
    pub items: Vec<AirQuality>,
    /// Total number of items BEFORE pagination was applied.
    pub total_count: i32,
    /// `true` when there are more items beyond the current page.
    pub has_next_page: bool,
}

// ── Energy ───────────────────────────────────────────────────────────────────

/// A single energy-consumption reading.
#[derive(Debug, Clone, SimpleObject)]
pub struct Energy {
    pub id: String,
    pub room_id: String,
    pub timestamp: DateTime<Utc>,
    /// Consumed energy (e.g. kWh).  Proto `double` maps to Rust `f64` and
    /// GraphQL `Float`.
    pub amount: f64,
}

/// Paginated list of `Energy` readings.
#[derive(Debug, SimpleObject)]
pub struct EnergyConnection {
    pub items: Vec<Energy>,
    pub total_count: i32,
    pub has_next_page: bool,
}

// ── Motion ───────────────────────────────────────────────────────────────────

/// A single motion-detection event.
#[derive(Debug, Clone, SimpleObject)]
pub struct Motion {
    pub id: String,
    pub room_id: String,
    pub timestamp: DateTime<Utc>,
    /// `true` when motion was detected.
    pub is_detected: bool,
}

/// Paginated list of `Motion` events.
#[derive(Debug, SimpleObject)]
pub struct MotionConnection {
    pub items: Vec<Motion>,
    pub total_count: i32,
    pub has_next_page: bool,
}

// ── Room ─────────────────────────────────────────────────────────────────────

/// Room metadata.
#[derive(Debug, Clone, SimpleObject)]
pub struct Room {
    pub id: String,
    pub name: String,
}

/// Paginated list of `Room` records.
#[derive(Debug, SimpleObject)]
pub struct RoomConnection {
    pub items: Vec<Room>,
    pub total_count: i32,
    pub has_next_page: bool,
}

// ── Aggregations ─────────────────────────────────────────────────────────────

/// Cross-service aggregation grouped by room.
///
/// Averages and counts are computed over all measurement services
/// (AirQuality, Energy, Motion) for the room within the requested time range.
///
/// `Option<f64>` (`Float` in GraphQL) means the field is `null` when no
/// readings of that type exist for the room — equivalent to `double?` in C#.
#[derive(Debug, SimpleObject)]
pub struct RoomAggregation {
    /// Room ID (join with the `room(id: …)` query to get the human name).
    pub room_id: String,
    /// Human-readable room name fetched from the Room service.
    pub room_name: Option<String>,
    pub avg_co2: Option<f64>,
    pub avg_pm25: Option<f64>,
    pub avg_humidity: Option<f64>,
    pub avg_energy: Option<f64>,
    /// Number of motion events where `is_detected = true`.
    pub motion_count: i32,
    /// Total number of data points (all types) for this room.
    pub total_count: i32,
}

/// Cross-service aggregation grouped by time bucket.
///
/// `interval_minutes` (default 60) controls the bucket width.  All
/// measurements whose timestamp falls in the same bucket are aggregated.
#[derive(Debug, SimpleObject)]
pub struct TimeAggregation {
    /// The start of the time bucket (aligned to the interval boundary).
    pub timestamp: DateTime<Utc>,
    pub avg_co2: Option<f64>,
    pub avg_pm25: Option<f64>,
    pub avg_humidity: Option<f64>,
    pub avg_energy: Option<f64>,
    pub motion_count: i32,
    pub total_count: i32,
}

// ============================================================================
// 3. DTO → GraphQL type converters
// ============================================================================

fn air_quality_from_dto(dto: AirQualityDto) -> AirQuality {
    AirQuality {
        id: dto.id,
        room_id: dto.room_id,
        timestamp: dto.timestamp,
        pm25: dto.pm25,
        co2: dto.co2,
        humidity: dto.humidity,
    }
}

fn energy_from_dto(dto: EnergyDto) -> Energy {
    Energy {
        id: dto.id,
        room_id: dto.room_id,
        timestamp: dto.timestamp,
        amount: dto.amount,
    }
}

fn motion_from_dto(dto: MotionDto) -> Motion {
    Motion {
        id: dto.id,
        room_id: dto.room_id,
        timestamp: dto.timestamp,
        is_detected: dto.is_detected,
    }
}

fn room_from_dto(dto: RoomDto) -> Room {
    Room {
        id: dto.id,
        name: dto.name,
    }
}

// ============================================================================
// 4. Pagination helpers
// ============================================================================
//
// Generic pagination applies offset + limit semantics — identical to LINQ's
// `.Skip(offset).Take(limit)` in C#.

fn paginate_air_quality(
    items: Vec<AirQuality>,
    pagination: &PaginationInput,
) -> AirQualityConnection {
    let (page_items, total_count, has_next_page) = apply_pagination(items, pagination);
    AirQualityConnection {
        items: page_items,
        total_count,
        has_next_page,
    }
}

fn paginate_energy(items: Vec<Energy>, pagination: &PaginationInput) -> EnergyConnection {
    let (page_items, total_count, has_next_page) = apply_pagination(items, pagination);
    EnergyConnection {
        items: page_items,
        total_count,
        has_next_page,
    }
}

fn paginate_motion(items: Vec<Motion>, pagination: &PaginationInput) -> MotionConnection {
    let (page_items, total_count, has_next_page) = apply_pagination(items, pagination);
    MotionConnection {
        items: page_items,
        total_count,
        has_next_page,
    }
}

fn paginate_room(items: Vec<Room>, pagination: &PaginationInput) -> RoomConnection {
    let (page_items, total_count, has_next_page) = apply_pagination(items, pagination);
    RoomConnection {
        items: page_items,
        total_count,
        has_next_page,
    }
}

/// Core pagination logic shared by all four list types.
///
/// Returns `(page_items, total_before_paging, has_next_page)`.
///
/// .NET parallel: `items.Skip(offset).Take(limit).ToList()`
fn apply_pagination<T>(items: Vec<T>, pagination: &PaginationInput) -> (Vec<T>, i32, bool) {
    let offset = pagination.offset.unwrap_or(0).max(0) as usize;
    let limit = pagination.limit.unwrap_or(20).max(1) as usize;
    let total_count = items.len() as i32;

    let has_next_page = (offset + limit) < items.len();
    let page_items = items.into_iter().skip(offset).take(limit).collect();

    (page_items, total_count, has_next_page)
}

// ============================================================================
// 5. Aggregation helpers
// ============================================================================

/// Compute the average of an iterator of `i32` values.
/// Returns `None` when the iterator is empty (no data → null in GraphQL).
fn avg_i32(iter: impl Iterator<Item = i32>) -> Option<f64> {
    let mut sum = 0.0f64;
    let mut count = 0u64;
    for v in iter {
        sum += v as f64;
        count += 1;
    }
    (count > 0).then(|| sum / count as f64)
}

/// Compute the average of an iterator of `f64` values.
fn avg_f64(iter: impl Iterator<Item = f64>) -> Option<f64> {
    let mut sum = 0.0f64;
    let mut count = 0u64;
    for v in iter {
        sum += v;
        count += 1;
    }
    (count > 0).then(|| sum / count as f64)
}

// ============================================================================
// 6. QueryRoot — GraphQL resolver methods
// ============================================================================
//
// HOW THIS WORKS FOR A .NET DEVELOPER
// ────────────────────────────────────
// `pub struct QueryRoot;` is a zero-sized "marker" struct.  The
// `#[Object] impl QueryRoot { … }` block registers every `async fn` inside it
// as a top-level GraphQL query field, exactly like marking methods with
// `[GraphQLQuery]` in Hot Chocolate or defining fields on `ObjectType<Query>`
// in GraphQL.NET.
//
// Accessing shared state (the gRPC client):
//   let client = ctx.data::<GrpcClient>()?;
// is equivalent to resolving a service from the ASP.NET Core DI container
// inside a resolver.  The `GrpcClient` is registered in `main.rs` via
// `Schema::build(...).data(grpc_client)`.
//
// Error handling:
//   All resolvers return `async_graphql::Result<T>`.  When a `?` propagates
//   an error, async-graphql converts it into a GraphQL error in the response.
//   This is analogous to an unhandled exception being caught by the GraphQL
//   execution engine and returned as an error array in the JSON response.
//
// Concurrent gRPC calls with `tokio::join!`:
//   The aggregation resolvers need data from multiple services at once.
//   `tokio::join!(future_a, future_b, future_c)` runs all three futures
//   concurrently and waits for all to complete — equivalent to
//   `await Task.WhenAll(taskA, taskB, taskC)` in C#.

// QueryRoot is the root object that holds all top-level GraphQL query
// fields. In async-graphql this is represented as an empty "marker" struct
// with an `#[Object] impl` block where each `async fn` becomes a GraphQL
// field (similar to methods on a QueryType class in Hot Chocolate).
//
// Important Rust-specific points for .NET developers:
//
// - Zero-sized marker: `pub struct QueryRoot;` has no fields — the resolvers
//   are the async methods implemented on the type via `#[Object] impl`.
// - Schema data & DI: the GraphQL `Schema` can hold arbitrary data via
//   `.data(...)`. Values stored there must be `Clone` (and usually `Send + Sync`).
//   When you call `let client = ctx.data::<GrpcClient>()?;` you are pulling
//   that cloned, shared instance out of the schema — think of it as resolving
//   a service from the DI container in ASP.NET Core.
// - Lifetimes & ownership: when you `.map(...)` or `.into_iter()` you will
//   often move ownership of values. Converters (DTO → GraphQL types) typically
//   take ownership to avoid unnecessary clones, and you will see `.clone()`
//   only when a cheap reference clone is required (e.g. tonic `Channel` clones).
// - Async & errors: resolver methods return `async_graphql::Result<T>`.
//   Use of `?` inside resolvers converts Rust errors into GraphQL errors,
//   so error propagation resembles throwing exceptions from resolvers in C#.
// - Concurrency: use `tokio::join!` to run multiple async RPCs concurrently
//   (equivalent to `Task.WhenAll`). The code intentionally collects all
//   results and converts each `Result` into GraphQL errors independently so
//   you get per-service error context.
//
// With these points in mind, reading the `impl QueryRoot { ... }` block is
// similar to reading a set of asynchronous controller actions that call
// typed gRPC clients, transform results to DTOs, and return GraphQL objects.
pub struct QueryRoot;

#[Object]
impl QueryRoot {
    // =========================================================================
    // Air Quality queries
    // =========================================================================

    /// Returns a paginated list of air-quality readings, optionally filtered
    /// by time range and/or room.
    ///
    /// Example:
    /// ```graphql
    /// query {
    ///   airQualities(filter: { roomId: "abc" }, pagination: { limit: 10 }) {
    ///     items { id co2 pm25 humidity timestamp }
    ///     totalCount hasNextPage
    ///   }
    /// }
    /// ```
    async fn air_qualities(
        &self,
        ctx: &Context<'_>,
        filter: Option<SensorFilter>,
        pagination: Option<PaginationInput>,
    ) -> async_graphql::Result<AirQualityConnection> {
        let client = ctx.data::<GrpcClient>()?;
        let filter = filter.unwrap_or_default();
        let pagination = pagination.unwrap_or_default();

        let dtos = client
            .get_air_qualities(filter.timestamp_start, filter.timestamp_end, filter.room_id)
            .await
            .map_err(|e| gql_grpc_err("airQualities", e))?;

        let items: Vec<AirQuality> = dtos.into_iter().map(air_quality_from_dto).collect();
        Ok(paginate_air_quality(items, &pagination))
    }

    /// Returns a single air-quality reading by its ID.
    ///
    /// Returns `null` wrapped in a GraphQL error if the record is not found.
    async fn air_quality(
        &self,
        ctx: &Context<'_>,
        id: String,
    ) -> async_graphql::Result<AirQuality> {
        let client = ctx.data::<GrpcClient>()?;

        client
            .get_air_quality(id)
            .await
            .map(air_quality_from_dto)
            .map_err(|e| gql_grpc_err("airQuality", e))
    }

    // =========================================================================
    // Energy queries
    // =========================================================================

    /// Returns a paginated list of energy-consumption readings.
    async fn energies(
        &self,
        ctx: &Context<'_>,
        filter: Option<SensorFilter>,
        pagination: Option<PaginationInput>,
    ) -> async_graphql::Result<EnergyConnection> {
        let client = ctx.data::<GrpcClient>()?;
        let filter = filter.unwrap_or_default();
        let pagination = pagination.unwrap_or_default();

        let dtos = client
            .get_energies(filter.timestamp_start, filter.timestamp_end, filter.room_id)
            .await
            .map_err(|e| gql_grpc_err("energies", e))?;

        let items: Vec<Energy> = dtos.into_iter().map(energy_from_dto).collect();
        Ok(paginate_energy(items, &pagination))
    }

    /// Returns a single energy reading by its ID.
    async fn energy(&self, ctx: &Context<'_>, id: String) -> async_graphql::Result<Energy> {
        let client = ctx.data::<GrpcClient>()?;

        client
            .get_energy(id)
            .await
            .map(energy_from_dto)
            .map_err(|e| gql_grpc_err("energy", e))
    }

    // =========================================================================
    // Motion queries
    // =========================================================================

    /// Returns a paginated list of motion-detection events.
    ///
    /// Use `filter.isDetected` to retrieve only "motion detected" or
    /// "no motion" events.  This filter is applied on the gateway side because
    /// the `GetMotionsRequest` proto message does not carry that field.
    async fn motions(
        &self,
        ctx: &Context<'_>,
        filter: Option<MotionFilter>,
        pagination: Option<PaginationInput>,
    ) -> async_graphql::Result<MotionConnection> {
        let client = ctx.data::<GrpcClient>()?;
        let filter = filter.unwrap_or_default();
        let pagination = pagination.unwrap_or_default();

        let dtos = client
            .get_motions(filter.timestamp_start, filter.timestamp_end, filter.room_id)
            .await
            .map_err(|e| gql_grpc_err("motions", e))?;

        // Convert to GraphQL type first, then apply the client-side
        // `is_detected` filter that the server doesn't support natively.
        let mut items: Vec<Motion> = dtos.into_iter().map(motion_from_dto).collect();

        if let Some(detected) = filter.is_detected {
            items.retain(|m| m.is_detected == detected);
        }

        Ok(paginate_motion(items, &pagination))
    }

    /// Returns a single motion event by its ID.
    async fn motion(&self, ctx: &Context<'_>, id: String) -> async_graphql::Result<Motion> {
        let client = ctx.data::<GrpcClient>()?;

        client
            .get_motion(id)
            .await
            .map(motion_from_dto)
            .map_err(|e| gql_grpc_err("motion", e))
    }

    // =========================================================================
    // Room queries
    // =========================================================================

    /// Returns a paginated list of rooms.
    ///
    /// The optional time range filters rooms by activity (rooms that had at
    /// least one measurement in the window), as defined by the proto contract.
    async fn rooms(
        &self,
        ctx: &Context<'_>,
        filter: Option<RoomFilter>,
        pagination: Option<PaginationInput>,
    ) -> async_graphql::Result<RoomConnection> {
        let client = ctx.data::<GrpcClient>()?;
        let filter = filter.unwrap_or_default();
        let pagination = pagination.unwrap_or_default();

        let dtos = client
            .get_rooms(filter.timestamp_start, filter.timestamp_end)
            .await
            .map_err(|e| gql_grpc_err("rooms", e))?;

        let items: Vec<Room> = dtos.into_iter().map(room_from_dto).collect();
        Ok(paginate_room(items, &pagination))
    }

    /// Returns a single room by its ID.
    async fn room(&self, ctx: &Context<'_>, id: String) -> async_graphql::Result<Room> {
        let client = ctx.data::<GrpcClient>()?;

        client
            .get_room(id)
            .await
            .map(room_from_dto)
            .map_err(|e| gql_grpc_err("room", e))
    }

    // =========================================================================
    // Aggregation queries
    // =========================================================================

    /// Aggregates all sensor data grouped by room.
    ///
    /// Fetches air-quality, energy, and motion data from three separate gRPC
    /// services **concurrently** (equivalent to `Task.WhenAll` in C#), then
    /// groups by `room_id` and computes averages and counts.
    ///
    /// The room name is resolved from the Room service and included in the
    /// result for display purposes.
    ///
    /// Example:
    /// ```graphql
    /// query {
    ///   aggregateByRoom(startTime: "2024-01-01T00:00:00Z") {
    ///     roomId roomName avgCo2 avgPm25 avgHumidity avgEnergy motionCount
    ///   }
    /// }
    /// ```
    async fn aggregate_by_room(
        &self,
        ctx: &Context<'_>,
        // Optional room filter — when set, only that room is aggregated.
        room_id: Option<String>,
        start_time: Option<DateTime<Utc>>,
        end_time: Option<DateTime<Utc>>,
    ) -> async_graphql::Result<Vec<RoomAggregation>> {
        let client = ctx.data::<GrpcClient>()?;

        // Run all four service calls concurrently.
        //
        // `tokio::join!(a, b, c, d)` polls all futures simultaneously and
        // returns a tuple of their results once every one has completed.
        // This is the Rust equivalent of:
        //   var (aq, en, mo, ro) = await (
        //       Task.WhenAll(aqTask, enTask, moTask, roTask));
        //
        // We use the non-error-short-circuiting `join!` (not `try_join!`) so
        // that we receive all four results and can convert errors individually.
        let (aq_result, en_result, mo_result, room_result) = tokio::join!(
            client.get_air_qualities(start_time, end_time, room_id.clone()),
            client.get_energies(start_time, end_time, room_id.clone()),
            client.get_motions(start_time, end_time, room_id.clone()),
            // Rooms have no room_id filter in the proto — fetch all and join.
            client.get_rooms(start_time, end_time),
        );

        // Convert each Result, propagating errors as GraphQL errors.
        // `.map_err(...)` is analogous to wrapping a caught exception into a
        // domain error in C#.
        let air_qualities =
            aq_result.map_err(|e| gql_grpc_err("aggregateByRoom → GetAirQualities", e))?;
        let energies = en_result.map_err(|e| gql_grpc_err("aggregateByRoom → GetEnergies", e))?;
        let motions = mo_result.map_err(|e| gql_grpc_err("aggregateByRoom → GetMotions", e))?;
        let rooms = room_result.map_err(|e| gql_grpc_err("aggregateByRoom → GetRooms", e))?;

        // Build a lookup map: room_id → room_name for O(1) joins later.
        // .NET parallel: rooms.ToDictionary(r => r.Id, r => r.Name)
        let room_names: HashMap<String, String> =
            rooms.into_iter().map(|r| (r.id, r.name)).collect();

        // ── Group all measurements by room_id ─────────────────────────────

        // HashMap<room_id, (Vec<co2>, Vec<pm25>, Vec<humidity>, Vec<amount>, motion_count)>
        // We use a single pass per service to accumulate per-room data.

        // Use a BTreeMap so the output order is deterministic (sorted by room_id).
        // .NET parallel: GroupBy(x => x.RoomId) + aggregation.
        let mut co2_by_room: BTreeMap<String, Vec<i32>> = BTreeMap::new();
        let mut pm25_by_room: BTreeMap<String, Vec<i32>> = BTreeMap::new();
        let mut humidity_by_room: BTreeMap<String, Vec<i32>> = BTreeMap::new();
        let mut energy_by_room: BTreeMap<String, Vec<f64>> = BTreeMap::new();
        let mut motion_by_room: BTreeMap<String, i32> = BTreeMap::new();
        let mut total_by_room: BTreeMap<String, i32> = BTreeMap::new();

        for aq in &air_qualities {
            co2_by_room
                .entry(aq.room_id.clone())
                .or_default()
                .push(aq.co2);
            pm25_by_room
                .entry(aq.room_id.clone())
                .or_default()
                .push(aq.pm25);
            humidity_by_room
                .entry(aq.room_id.clone())
                .or_default()
                .push(aq.humidity);
            *total_by_room.entry(aq.room_id.clone()).or_default() += 1;
        }

        for en in &energies {
            energy_by_room
                .entry(en.room_id.clone())
                .or_default()
                .push(en.amount);
            *total_by_room.entry(en.room_id.clone()).or_default() += 1;
        }

        for mo in &motions {
            if mo.is_detected {
                *motion_by_room.entry(mo.room_id.clone()).or_default() += 1;
            }
            *total_by_room.entry(mo.room_id.clone()).or_default() += 1;
        }

        // Collect all distinct room_ids seen across all services.
        let mut all_room_ids: std::collections::BTreeSet<String> =
            total_by_room.keys().cloned().collect();

        // If the caller requested a specific room_id but it had no data, still
        // include it (with null averages) so the caller gets a defined result.
        if let Some(ref rid) = room_id {
            all_room_ids.insert(rid.clone());
        }

        // ── Build aggregation objects ──────────────────────────────────────

        let result = all_room_ids
            .into_iter()
            .map(|rid| {
                let avg_co2 = co2_by_room
                    .get(&rid)
                    .and_then(|v| avg_i32(v.iter().copied()));

                let avg_pm25 = pm25_by_room
                    .get(&rid)
                    .and_then(|v| avg_i32(v.iter().copied()));

                let avg_humidity = humidity_by_room
                    .get(&rid)
                    .and_then(|v| avg_i32(v.iter().copied()));

                let avg_energy = energy_by_room
                    .get(&rid)
                    .and_then(|v| avg_f64(v.iter().copied()));

                RoomAggregation {
                    room_name: room_names.get(&rid).cloned(),
                    room_id: rid.clone(),
                    avg_co2,
                    avg_pm25,
                    avg_humidity,
                    avg_energy,
                    motion_count: *motion_by_room.get(&rid).unwrap_or(&0),
                    total_count: *total_by_room.get(&rid).unwrap_or(&0),
                }
            })
            .collect();

        Ok(result)
    }

    /// Aggregates all sensor data grouped by time buckets.
    ///
    /// `interval_minutes` controls the bucket width (default: 60 minutes).
    /// Timestamps are aligned to bucket boundaries (floor division).
    ///
    /// Like `aggregateByRoom`, this fetches from three services concurrently.
    ///
    /// Example:
    /// ```graphql
    /// query {
    ///   aggregateByTime(intervalMinutes: 30) {
    ///     timestamp avgCo2 avgEnergy motionCount
    ///   }
    /// }
    /// ```
    async fn aggregate_by_time(
        &self,
        ctx: &Context<'_>,
        // Optional room filter — when set, only that room's data is used.
        room_id: Option<String>,
        start_time: Option<DateTime<Utc>>,
        end_time: Option<DateTime<Utc>>,
        // Bucket width in minutes (default: 60).
        interval_minutes: Option<i32>,
    ) -> async_graphql::Result<Vec<TimeAggregation>> {
        let client = ctx.data::<GrpcClient>()?;

        // Fetch all three measurement services concurrently.
        let (aq_result, en_result, mo_result) = tokio::join!(
            client.get_air_qualities(start_time, end_time, room_id.clone()),
            client.get_energies(start_time, end_time, room_id.clone()),
            client.get_motions(start_time, end_time, room_id),
        );

        let air_qualities =
            aq_result.map_err(|e| gql_grpc_err("aggregateByTime → GetAirQualities", e))?;
        let energies = en_result.map_err(|e| gql_grpc_err("aggregateByTime → GetEnergies", e))?;
        let motions = mo_result.map_err(|e| gql_grpc_err("aggregateByTime → GetMotions", e))?;

        // Bucket width in seconds.  `.max(1)` prevents division by zero.
        // .NET parallel: TimeSpan.FromMinutes(intervalMinutes)
        let interval_secs = (interval_minutes.unwrap_or(60).max(1) as i64) * 60;

        // Aligns a Unix timestamp to the start of its bucket.
        // E.g. for a 60-minute bucket: 14:37 → 14:00.
        // .NET parallel: DateTimeOffset.FromUnixTimeSeconds(
        //     (ts / intervalSecs) * intervalSecs)
        let bucket = |ts: i64| -> i64 { (ts / interval_secs) * interval_secs };

        // BTreeMap keeps buckets sorted chronologically.
        let mut co2_buckets: BTreeMap<i64, Vec<i32>> = BTreeMap::new();
        let mut pm25_buckets: BTreeMap<i64, Vec<i32>> = BTreeMap::new();
        let mut humidity_buckets: BTreeMap<i64, Vec<i32>> = BTreeMap::new();
        let mut energy_buckets: BTreeMap<i64, Vec<f64>> = BTreeMap::new();
        let mut motion_buckets: BTreeMap<i64, i32> = BTreeMap::new();
        let mut total_buckets: BTreeMap<i64, i32> = BTreeMap::new();

        for aq in &air_qualities {
            let b = bucket(aq.timestamp.timestamp());
            co2_buckets.entry(b).or_default().push(aq.co2);
            pm25_buckets.entry(b).or_default().push(aq.pm25);
            humidity_buckets.entry(b).or_default().push(aq.humidity);
            *total_buckets.entry(b).or_default() += 1;
        }

        for en in &energies {
            let b = bucket(en.timestamp.timestamp());
            energy_buckets.entry(b).or_default().push(en.amount);
            *total_buckets.entry(b).or_default() += 1;
        }

        for mo in &motions {
            let b = bucket(mo.timestamp.timestamp());
            if mo.is_detected {
                *motion_buckets.entry(b).or_default() += 1;
            }
            *total_buckets.entry(b).or_default() += 1;
        }

        // Collect all bucket timestamps seen across all services.
        let all_buckets: std::collections::BTreeSet<i64> = total_buckets.keys().copied().collect();

        let result = all_buckets
            .into_iter()
            .map(|b| {
                let avg_co2 = co2_buckets
                    .get(&b)
                    .and_then(|v| avg_i32(v.iter().copied()));

                let avg_pm25 = pm25_buckets
                    .get(&b)
                    .and_then(|v| avg_i32(v.iter().copied()));

                let avg_humidity = humidity_buckets
                    .get(&b)
                    .and_then(|v| avg_i32(v.iter().copied()));

                let avg_energy = energy_buckets
                    .get(&b)
                    .and_then(|v| avg_f64(v.iter().copied()));

                // Convert the Unix-epoch bucket start back to a DateTime<Utc>.
                // .NET: DateTimeOffset.FromUnixTimeSeconds(b).UtcDateTime
                let timestamp = DateTime::from_timestamp(b, 0).unwrap_or_default();

                TimeAggregation {
                    timestamp,
                    avg_co2,
                    avg_pm25,
                    avg_humidity,
                    avg_energy,
                    motion_count: *motion_buckets.get(&b).unwrap_or(&0),
                    total_count: *total_buckets.get(&b).unwrap_or(&0),
                }
            })
            .collect();

        Ok(result)
    }
}
