export interface PaginationInput {
  offset?: number;
  limit?: number;
}

export interface SensorFilter {
  timestampStart?: string;
  timestampEnd?: string;
  roomId?: string;
}

export interface MotionFilter {
  timestampStart?: string;
  timestampEnd?: string;
  roomId?: string;
  isDetected?: boolean;
}

export interface AirQuality {
  id: string;
  roomId: string;
  timestamp: string;
  pm25: number;
  co2: number;
  humidity: number;
}

export interface AirQualityConnection {
  items: AirQuality[];
  totalCount: number;
  hasNextPage: boolean;
}

export interface Energy {
  id: string;
  roomId: string;
  timestamp: string;
  amount: number;
}

export interface EnergyConnection {
  items: Energy[];
  totalCount: number;
  hasNextPage: boolean;
}

export interface Motion {
  id: string;
  roomId: string;
  timestamp: string;
  isDetected: boolean;
}

export interface MotionConnection {
  items: Motion[];
  totalCount: number;
  hasNextPage: boolean;
}

export interface Room {
  id: string;
  name: string;
}

export interface RoomConnection {
  items: Room[];
  totalCount: number;
  hasNextPage: boolean;
}

export interface RoomAggregation {
  roomId: string;
  roomName: string | null;
  avgCo2: number | null;
  avgPm25: number | null;
  avgHumidity: number | null;
  avgEnergy: number | null;
  motionCount: number;
  totalCount: number;
}

export interface TimeAggregation {
  timestamp: string;
  avgCo2: number | null;
  avgPm25: number | null;
  avgHumidity: number | null;
  avgEnergy: number | null;
  motionCount: number;
  totalCount: number;
}

export interface MotionEvent {
  roomName: string;
  isDetected: boolean;
  timestamp: string;
}

export interface AggregateByRoomResponse {
  aggregateByRoom: RoomAggregation[];
}

export interface AggregateByTimeResponse {
  aggregateByTime: TimeAggregation[];
}

export interface AirQualitiesQueryResponse {
  airQualities: AirQualityConnection;
}

export interface EnergiesQueryResponse {
  energies: EnergyConnection;
}

export interface MotionsQueryResponse {
  motions: MotionConnection;
}

export interface RoomsQueryResponse {
  rooms: RoomConnection;
}
