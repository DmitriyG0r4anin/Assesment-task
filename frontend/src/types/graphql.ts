import { gql } from 'graphql-request';

export const AGGREGATE_BY_ROOM = gql`
    query AggregateByRoom($roomId: String, $startTime: DateTime, $endTime: DateTime) {
        aggregateByRoom(roomId: $roomId, startTime: $startTime, endTime: $endTime) {
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
`;

export const ROOMS_QUERY = gql`
    query RoomsForFilter($pagination: PaginationInput) {
        rooms(pagination: $pagination) {
            items {
                id
                name
            }
            totalCount
        }
    }
`;

export const AIR_QUERY = gql`
    query AirQualities($filter: SensorFilter, $pagination: PaginationInput) {
        airQualities(filter: $filter, pagination: $pagination) {
            items {
                id
                roomId
                timestamp
                pm25
                co2
                humidity
            }
            totalCount
            hasNextPage
        }
    }
`;

export const ENERGY_QUERY = gql`
    query Energies($filter: SensorFilter, $pagination: PaginationInput) {
        energies(filter: $filter, pagination: $pagination) {
            items {
                id
                roomId
                timestamp
                amount
            }
            totalCount
            hasNextPage
        }
    }
`;

export const MOTIONS_QUERY = gql`
    query Motions($filter: MotionFilter, $pagination: PaginationInput) {
        motions(filter: $filter, pagination: $pagination) {
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
`;
