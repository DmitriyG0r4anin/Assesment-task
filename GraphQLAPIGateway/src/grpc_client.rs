
use chrono::{DateTime, Utc};
use tonic::transport::{Channel, ClientTlsConfig};
use tracing::instrument;

pub mod proto {
    tonic::include_proto!("parameters");
}

use proto::air_quality_service_client::AirQualityServiceClient;
use proto::energy_service_client::EnergyServiceClient;
use proto::motion_service_client::MotionServiceClient;
use proto::room_service_client::RoomServiceClient;


#[derive(Debug, Clone)]
pub struct AirQualityDto {
    pub id: String,
    pub room_id: String,
    pub timestamp: DateTime<Utc>,
    pub pm25: i32,     
    pub co2: i32,      
    pub humidity: i32, 
}

#[derive(Debug, Clone)]
pub struct EnergyDto {
    pub id: String,
    pub room_id: String,
    pub timestamp: DateTime<Utc>,
    pub amount: f64,
}

#[derive(Debug, Clone)]
pub struct MotionDto {
    pub id: String,
    pub room_id: String,
    pub timestamp: DateTime<Utc>,
    pub is_detected: bool,
}

#[derive(Debug, Clone)]
pub struct RoomDto {
    pub id: String,
    pub name: String,
}

fn proto_ts_to_chrono(ts: &prost_types::Timestamp) -> DateTime<Utc> {
    DateTime::from_timestamp(ts.seconds, ts.nanos as u32).unwrap_or_default()
}

fn chrono_to_proto_ts(dt: &DateTime<Utc>) -> prost_types::Timestamp {
    prost_types::Timestamp {
        seconds: dt.timestamp(),
        nanos: dt.timestamp_subsec_nanos() as i32,
    }
}

fn air_quality_msg_to_dto(msg: proto::AirQualityMessage) -> AirQualityDto {
    AirQualityDto {
        id: msg.id,
        room_id: msg.room_id,
        timestamp: msg
            .timestamp
            .as_ref()
            .map(proto_ts_to_chrono)
            .unwrap_or_default(),
        pm25: msg.pm25,
        co2: msg.co2,
        humidity: msg.humidity,
    }
}

fn energy_msg_to_dto(msg: proto::EnergyMessage) -> EnergyDto {
    EnergyDto {
        id: msg.id,
        room_id: msg.room_id,
        timestamp: msg
            .timestamp
            .as_ref()
            .map(proto_ts_to_chrono)
            .unwrap_or_default(),
        amount: msg.amount,
    }
}

fn motion_msg_to_dto(msg: proto::MotionMessage) -> MotionDto {
    MotionDto {
        id: msg.id,
        room_id: msg.room_id,
        timestamp: msg
            .timestamp
            .as_ref()
            .map(proto_ts_to_chrono)
            .unwrap_or_default(),
        is_detected: msg.is_detected,
    }
}

fn room_msg_to_dto(msg: proto::RoomMessage) -> RoomDto {
    RoomDto {
        id: msg.id,
        name: msg.name,
    }
}

type ClientError = Box<dyn std::error::Error + Send + Sync>;

#[derive(Debug, Clone)]
pub struct GrpcClient {
    air_quality: AirQualityServiceClient<Channel>,
    energy: EnergyServiceClient<Channel>,
    motion: MotionServiceClient<Channel>,
    room: RoomServiceClient<Channel>,
}

impl GrpcClient {
    #[instrument(skip(), fields(endpoint = %endpoint))]
    pub async fn connect(endpoint: String) -> Result<Self, Box<dyn std::error::Error>> {
        tracing::info!("resolving endpoint and opening HTTP/2 channel (TLS if https)");
        let mut ep = Channel::from_shared(endpoint)
            .map_err(|e| -> Box<dyn std::error::Error> { Box::new(e) })?;

        if ep.uri().scheme_str() == Some("https") {
            tracing::debug!("applying ClientTlsConfig with enabled root CAs for HTTPS");
            ep = ep
                .tls_config(ClientTlsConfig::new().with_enabled_roots())
                .map_err(|e| -> Box<dyn std::error::Error> { Box::new(e) })?;
        }

        let channel = ep
            .connect()
            .await
            .map_err(|e| -> Box<dyn std::error::Error> { Box::new(e) })?;

        tracing::info!("connected; wiring AirQuality, Energy, Motion, and Room clients");

        Ok(Self {
            air_quality: AirQualityServiceClient::new(channel.clone()),
            energy: EnergyServiceClient::new(channel.clone()),
            motion: MotionServiceClient::new(channel.clone()),
            room: RoomServiceClient::new(channel),
        })
    }

    #[instrument(skip(self), level = "debug", err)]
    pub async fn get_air_qualities(
        &self,
        timestamp_start: Option<DateTime<Utc>>,
        timestamp_end: Option<DateTime<Utc>>,
        room_id: Option<String>,
    ) -> Result<Vec<AirQualityDto>, ClientError> {
        let request = proto::GetAirQualitiesRequest {
            timestamp_start: timestamp_start.as_ref().map(chrono_to_proto_ts),
            timestamp_end: timestamp_end.as_ref().map(chrono_to_proto_ts),
            room_id,
        };

        let response = self
            .air_quality
            .clone()
            .get_air_qualities(tonic::Request::new(request))
            .await?
            .into_inner(); // unwrap the tonic::Response wrapper

        match response.result {
            Some(proto::get_air_qualities_response::Result::Data(list)) => {
                Ok(list
                    .air_qualities
                    .into_iter()
                    .map(air_quality_msg_to_dto)
                    .collect())
            }
            Some(proto::get_air_qualities_response::Result::Error(err)) => {
                Err(format!("gRPC error (code {}): {}", err.code, err.message).into())
            }
            None => Ok(vec![]),
        }
    }

    #[instrument(skip(self), level = "debug", err)]
    pub async fn get_air_quality(
        &self,
        air_quality_id: String,
    ) -> Result<AirQualityDto, ClientError> {
        let request = proto::GetAirQualityRequest { air_quality_id };

        let response = self
            .air_quality
            .clone()
            .get_air_quality(tonic::Request::new(request))
            .await?
            .into_inner();

        match response.result {
            Some(proto::get_air_quality_response::Result::Data(msg)) => {
                Ok(air_quality_msg_to_dto(msg))
            }
            Some(proto::get_air_quality_response::Result::Error(err)) => {
                Err(format!("gRPC error (code {}): {}", err.code, err.message).into())
            }
            None => Err("GetAirQuality returned no data".into()),
        }
    }

    #[instrument(skip(self), level = "debug", err)]
    pub async fn get_energies(
        &self,
        timestamp_start: Option<DateTime<Utc>>,
        timestamp_end: Option<DateTime<Utc>>,
        room_id: Option<String>,
    ) -> Result<Vec<EnergyDto>, ClientError> {
        let request = proto::GetEnergiesRequest {
            timestamp_start: timestamp_start.as_ref().map(chrono_to_proto_ts),
            timestamp_end: timestamp_end.as_ref().map(chrono_to_proto_ts),
            room_id,
        };

        let response = self
            .energy
            .clone()
            .get_energies(tonic::Request::new(request))
            .await?
            .into_inner();

        match response.result {
            Some(proto::get_energies_response::Result::Data(list)) => {
                Ok(list.energies.into_iter().map(energy_msg_to_dto).collect())
            }
            Some(proto::get_energies_response::Result::Error(err)) => {
                Err(format!("gRPC error (code {}): {}", err.code, err.message).into())
            }
            None => Ok(vec![]),
        }
    }

    #[instrument(skip(self), level = "debug", err)]
    pub async fn get_energy(&self, energy_id: String) -> Result<EnergyDto, ClientError> {
        let request = proto::GetEnergyRequest { energy_id };

        let response = self
            .energy
            .clone()
            .get_energy(tonic::Request::new(request))
            .await?
            .into_inner();

        match response.result {
            Some(proto::get_energy_response::Result::Data(msg)) => Ok(energy_msg_to_dto(msg)),
            Some(proto::get_energy_response::Result::Error(err)) => {
                Err(format!("gRPC error (code {}): {}", err.code, err.message).into())
            }
            None => Err("GetEnergy returned no data".into()),
        }
    }

    #[instrument(skip(self), level = "debug", err)]
    pub async fn get_motions(
        &self,
        timestamp_start: Option<DateTime<Utc>>,
        timestamp_end: Option<DateTime<Utc>>,
        room_id: Option<String>,
    ) -> Result<Vec<MotionDto>, ClientError> {
        let request = proto::GetMotionsRequest {
            timestamp_start: timestamp_start.as_ref().map(chrono_to_proto_ts),
            timestamp_end: timestamp_end.as_ref().map(chrono_to_proto_ts),
            room_id,
        };

        let response = self
            .motion
            .clone()
            .get_motions(tonic::Request::new(request))
            .await?
            .into_inner();

        match response.result {
            Some(proto::get_motions_response::Result::Data(list)) => {
                Ok(list.motions.into_iter().map(motion_msg_to_dto).collect())
            }
            Some(proto::get_motions_response::Result::Error(err)) => {
                Err(format!("gRPC error (code {}): {}", err.code, err.message).into())
            }
            None => Ok(vec![]),
        }
    }

    #[instrument(skip(self), level = "debug", err)]
    pub async fn get_motion(&self, motion_id: String) -> Result<MotionDto, ClientError> {
        let request = proto::GetMotionRequest { motion_id };

        let response = self
            .motion
            .clone()
            .get_motion(tonic::Request::new(request))
            .await?
            .into_inner();

        match response.result {
            Some(proto::get_motion_response::Result::Data(msg)) => Ok(motion_msg_to_dto(msg)),
            Some(proto::get_motion_response::Result::Error(err)) => {
                Err(format!("gRPC error (code {}): {}", err.code, err.message).into())
            }
            None => Err("GetMotion returned no data".into()),
        }
    }

    #[instrument(skip(self), level = "debug", err)]
    pub async fn get_rooms(
        &self,
        timestamp_start: Option<DateTime<Utc>>,
        timestamp_end: Option<DateTime<Utc>>,
    ) -> Result<Vec<RoomDto>, ClientError> {
        let request = proto::GetRoomsRequest {
            timestamp_start: timestamp_start.as_ref().map(chrono_to_proto_ts),
            timestamp_end: timestamp_end.as_ref().map(chrono_to_proto_ts),
        };

        let response = self
            .room
            .clone()
            .get_rooms(tonic::Request::new(request))
            .await?
            .into_inner();

        match response.result {
            Some(proto::get_rooms_response::Result::Data(list)) => {
                Ok(list.rooms.into_iter().map(room_msg_to_dto).collect())
            }
            Some(proto::get_rooms_response::Result::Error(err)) => {
                Err(format!("gRPC error (code {}): {}", err.code, err.message).into())
            }
            None => Ok(vec![]),
        }
    }

    #[instrument(skip(self), level = "debug", err)]
    pub async fn get_room(&self, room_id: String) -> Result<RoomDto, ClientError> {
        let request = proto::GetRoomRequest { room_id };

        let response = self
            .room
            .clone()
            .get_room(tonic::Request::new(request))
            .await?
            .into_inner();

        match response.result {
            Some(proto::get_room_response::Result::Data(msg)) => Ok(room_msg_to_dto(msg)),
            Some(proto::get_room_response::Result::Error(err)) => {
                Err(format!("gRPC error (code {}): {}", err.code, err.message).into())
            }
            None => Err("GetRoom returned no data".into()),
        }
    }
}
