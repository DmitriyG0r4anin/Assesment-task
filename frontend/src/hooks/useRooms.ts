import { useEffect, useState } from 'react';
import { graphqlClient } from '../lib/graphql-client';
import type { Room, RoomsQueryResponse } from '../types';
import { ROOMS_QUERY } from '../types/graphql';

export function useRooms(): Room[] {
    const [rooms, setRooms] = useState<Room[]>([]);

    useEffect(() => {
        let cancelled = false;

        async function loadRooms(): Promise<void> {
            try {
                const data = await graphqlClient.request<RoomsQueryResponse>(ROOMS_QUERY, {
                    pagination: { offset: 0, limit: 500 },
                });
                if (!cancelled) {
                    setRooms(data.rooms.items);
                }
            } catch (error) {
                console.error('Failed to load rooms:', error);
            }
        }

        void loadRooms();
        return () => {
            cancelled = true;
        };
    }, []);

    return rooms;
}
