use std::collections::{BTreeMap, HashMap};

use async_graphql::{Context, InputObject, Object, SimpleObject};
use chrono::{DateTime, Utc};

use crate::grpc_client::{AirQualityDto, EnergyDto, GrpcClient, MotionDto, RoomDto};

#[inline]
fn gql_grpc_err(context: &'static str, e: impl std::fmt::Display) -> async_graphql::Error {
    tracing::error!(context, error = %e, "gRPC call failed");
    async_graphql::Error::new(e.to_string())
}

#[derive(Debug, Default, InputObject)]
pub struct SensorFilter {
    pub timestamp_start: Option<DateTime<Utc>>,
    pub timestamp_end: Option<DateTime<Utc>>,
    pub room_id: Option<String>,
}

#[derive(Debug, Default, InputObject)]
pub struct MotionFilter {
    pub timestamp_start: Option<DateTime<Utc>>,
    pub timestamp_end: Option<DateTime<Utc>>,
    pub room_id: Option<String>,
    pub is_detected: Option<bool>,
}

#[derive(Debug, Default, InputObject)]
pub struct RoomFilter {
    pub timestamp_start: Option<DateTime<Utc>>,
    pub timestamp_end: Option<DateTime<Utc>>,
}

#[derive(Debug, Default, InputObject)]
pub struct PaginationInput {
    pub offset: Option<i32>,
    pub limit: Option<i32>,
}

#[derive(Debug, Clone, SimpleObject)]
pub struct AirQuality {
    pub id: String,
    pub room_id: String,
    pub timestamp: DateTime<Utc>,
    pub pm25: i32,
    pub co2: i32,
    pub humidity: i32,
}

#[derive(Debug, SimpleObject)]
pub struct AirQualityConnection {
    pub items: Vec<AirQuality>,
    pub total_count: i32,
    pub has_next_page: bool,
}

#[derive(Debug, Clone, SimpleObject)]
pub struct Energy {
    pub id: String,
    pub room_id: String,
    pub timestamp: DateTime<Utc>,
    pub amount: f64,
}

#[derive(Debug, SimpleObject)]
pub struct EnergyConnection {
    pub items: Vec<Energy>,
    pub total_count: i32,
    pub has_next_page: bool,
}

#[derive(Debug, Clone, SimpleObject)]
pub struct Motion {
    pub id: String,
    pub room_id: String,
    pub timestamp: DateTime<Utc>,
    pub is_detected: bool,
}

#[derive(Debug, SimpleObject)]
pub struct MotionConnection {
    pub items: Vec<Motion>,
    pub total_count: i32,
    pub has_next_page: bool,
}

#[derive(Debug, Clone, SimpleObject)]
pub struct Room {
    pub id: String,
    pub name: String,
}

#[derive(Debug, SimpleObject)]
pub struct RoomConnection {
    pub items: Vec<Room>,
    pub total_count: i32,
    pub has_next_page: bool,
}

#[derive(Debug, SimpleObject)]
pub struct RoomAggregation {
    pub room_id: String,
    pub room_name: Option<String>,
    pub avg_co2: Option<f64>,
    pub avg_pm25: Option<f64>,
    pub avg_humidity: Option<f64>,
    pub avg_energy: Option<f64>,
    pub motion_count: i32,
    pub total_count: i32,
}

#[derive(Debug, SimpleObject)]
pub struct TimeAggregation {
    pub timestamp: DateTime<Utc>,
    pub avg_co2: Option<f64>,
    pub avg_pm25: Option<f64>,
    pub avg_humidity: Option<f64>,
    pub avg_energy: Option<f64>,
    pub motion_count: i32,
    pub total_count: i32,
}

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

fn apply_pagination<T>(items: Vec<T>, pagination: &PaginationInput) -> (Vec<T>, i32, bool) {
    let offset = pagination.offset.unwrap_or(0).max(0) as usize;
    let limit = pagination.limit.unwrap_or(20).max(1) as usize;
    let total_count = items.len() as i32;

    let has_next_page = (offset + limit) < items.len();
    let page_items = items.into_iter().skip(offset).take(limit).collect();

    (page_items, total_count, has_next_page)
}

fn avg_i32(iter: impl Iterator<Item = i32>) -> Option<f64> {
    let mut sum = 0.0f64;
    let mut count = 0u64;
    for v in iter {
        sum += v as f64;
        count += 1;
    }
    (count > 0).then(|| sum / count as f64)
}

fn avg_f64(iter: impl Iterator<Item = f64>) -> Option<f64> {
    let mut sum = 0.0f64;
    let mut count = 0u64;
    for v in iter {
        sum += v;
        count += 1;
    }
    (count > 0).then(|| sum / count as f64)
}

pub struct QueryRoot;

#[Object]
impl QueryRoot {
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

    async fn energy(&self, ctx: &Context<'_>, id: String) -> async_graphql::Result<Energy> {
        let client = ctx.data::<GrpcClient>()?;

        client
            .get_energy(id)
            .await
            .map(energy_from_dto)
            .map_err(|e| gql_grpc_err("energy", e))
    }

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

        let mut items: Vec<Motion> = dtos.into_iter().map(motion_from_dto).collect();

        if let Some(detected) = filter.is_detected {
            items.retain(|m| m.is_detected == detected);
        }

        Ok(paginate_motion(items, &pagination))
    }

    async fn motion(&self, ctx: &Context<'_>, id: String) -> async_graphql::Result<Motion> {
        let client = ctx.data::<GrpcClient>()?;

        client
            .get_motion(id)
            .await
            .map(motion_from_dto)
            .map_err(|e| gql_grpc_err("motion", e))
    }

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

    async fn room(&self, ctx: &Context<'_>, id: String) -> async_graphql::Result<Room> {
        let client = ctx.data::<GrpcClient>()?;

        client
            .get_room(id)
            .await
            .map(room_from_dto)
            .map_err(|e| gql_grpc_err("room", e))
    }

    async fn aggregate_by_room(
        &self,
        ctx: &Context<'_>,
        room_id: Option<String>,
        start_time: Option<DateTime<Utc>>,
        end_time: Option<DateTime<Utc>>,
    ) -> async_graphql::Result<Vec<RoomAggregation>> {
        let client = ctx.data::<GrpcClient>()?;

        let (aq_result, en_result, mo_result, room_result) = tokio::join!(
            client.get_air_qualities(start_time, end_time, room_id.clone()),
            client.get_energies(start_time, end_time, room_id.clone()),
            client.get_motions(start_time, end_time, room_id.clone()),
            client.get_rooms(start_time, end_time),
        );

        let air_qualities =
            aq_result.map_err(|e| gql_grpc_err("aggregateByRoom → GetAirQualities", e))?;
        let energies = en_result.map_err(|e| gql_grpc_err("aggregateByRoom → GetEnergies", e))?;
        let motions = mo_result.map_err(|e| gql_grpc_err("aggregateByRoom → GetMotions", e))?;
        let rooms = room_result.map_err(|e| gql_grpc_err("aggregateByRoom → GetRooms", e))?;

        let room_names: HashMap<String, String> =
            rooms.into_iter().map(|r| (r.id, r.name)).collect();

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

        let mut all_room_ids: std::collections::BTreeSet<String> =
            total_by_room.keys().cloned().collect();

        if let Some(ref rid) = room_id {
            all_room_ids.insert(rid.clone());
        }

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

    async fn aggregate_by_time(
        &self,
        ctx: &Context<'_>,
        room_id: Option<String>,
        start_time: Option<DateTime<Utc>>,
        end_time: Option<DateTime<Utc>>,
        interval_minutes: Option<i32>,
    ) -> async_graphql::Result<Vec<TimeAggregation>> {
        let client = ctx.data::<GrpcClient>()?;

        let (aq_result, en_result, mo_result) = tokio::join!(
            client.get_air_qualities(start_time, end_time, room_id.clone()),
            client.get_energies(start_time, end_time, room_id.clone()),
            client.get_motions(start_time, end_time, room_id),
        );

        let air_qualities =
            aq_result.map_err(|e| gql_grpc_err("aggregateByTime → GetAirQualities", e))?;
        let energies = en_result.map_err(|e| gql_grpc_err("aggregateByTime → GetEnergies", e))?;
        let motions = mo_result.map_err(|e| gql_grpc_err("aggregateByTime → GetMotions", e))?;

        let interval_secs = (interval_minutes.unwrap_or(60).max(1) as i64) * 60;

        let bucket = |ts: i64| -> i64 { (ts / interval_secs) * interval_secs };

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

        let all_buckets: std::collections::BTreeSet<i64> = total_buckets.keys().copied().collect();

        let result = all_buckets
            .into_iter()
            .map(|b| {
                let avg_co2 = co2_buckets.get(&b).and_then(|v| avg_i32(v.iter().copied()));

                let avg_pm25 = pm25_buckets
                    .get(&b)
                    .and_then(|v| avg_i32(v.iter().copied()));

                let avg_humidity = humidity_buckets
                    .get(&b)
                    .and_then(|v| avg_i32(v.iter().copied()));

                let avg_energy = energy_buckets
                    .get(&b)
                    .and_then(|v| avg_f64(v.iter().copied()));

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
