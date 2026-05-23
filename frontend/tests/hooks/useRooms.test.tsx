vi.mock('@/types/config', () => ({
    default: {
        graphqlUrl: 'http://localhost:4000/graphql',
    },
}));

const requestMock = vi.fn();

vi.mock('@/lib/graphql-client', () => ({
    graphqlClient: {
        request: (...args: unknown[]) => requestMock(...args),
    },
}));

import { renderHook, waitFor } from '@testing-library/react';
import { useRooms } from '@/hooks/useRooms';
import { ROOMS_QUERY } from '@/types/graphql';

describe('useRooms', () => {
    beforeEach(() => {
        requestMock.mockReset();
    });

    it('loads rooms from the GraphQL API', async () => {
        requestMock.mockResolvedValue({
            rooms: {
                items: [
                    { id: 'room-1', name: 'Kitchen' },
                    { id: 'room-2', name: 'Office' },
                ],
            },
        });

        const { result } = renderHook(() => useRooms());

        await waitFor(() => {
            expect(result.current).toEqual([
                { id: 'room-1', name: 'Kitchen' },
                { id: 'room-2', name: 'Office' },
            ]);
        });

        expect(requestMock).toHaveBeenCalledWith(ROOMS_QUERY, {
            pagination: { offset: 0, limit: 500 },
        });
    });

    it('logs errors and keeps an empty list when loading fails', async () => {
        vi.spyOn(console, 'error').mockImplementation(() => undefined);
        requestMock.mockRejectedValue(new Error('network error'));

        const { result } = renderHook(() => useRooms());

        await waitFor(() => {
            expect(requestMock).toHaveBeenCalled();
        });

        expect(result.current).toEqual([]);
        expect(console.error).toHaveBeenCalledWith('Failed to load rooms:', expect.any(Error));
    });
});
