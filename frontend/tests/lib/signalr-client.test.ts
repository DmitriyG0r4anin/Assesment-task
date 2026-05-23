import { createSignalRConnection, startConnection } from '@/lib/signalr-client';

vi.mock('@microsoft/signalr', () => {
    const connection = {
        start: vi.fn(),
    };

    class MockHubConnectionBuilder {
        withUrl = vi.fn().mockReturnThis();
        withAutomaticReconnect = vi.fn().mockReturnThis();
        configureLogging = vi.fn().mockReturnThis();
        build = vi.fn(() => connection);
    }

    return {
        HubConnectionBuilder: MockHubConnectionBuilder,
        HttpTransportType: { WebSockets: 1 },
        LogLevel: { Information: 3, Warning: 2 },
    };
});

vi.mock('@/types/config', () => ({
    default: {
        notificationsUrl: 'http://localhost:8092/notifications/motionHub',
    },
}));

describe('createSignalRConnection', () => {
    it('builds a websocket connection with automatic reconnect', () => {
        const connection = createSignalRConnection();

        expect(connection.start).toBeDefined();
    });
});

describe('startConnection', () => {
    beforeEach(() => {
        vi.useFakeTimers();
        vi.spyOn(console, 'log').mockImplementation(() => undefined);
        vi.spyOn(console, 'error').mockImplementation(() => undefined);
    });

    afterEach(() => {
        vi.useRealTimers();
        vi.restoreAllMocks();
    });

    it('does nothing when the consumer is already disposed', async () => {
        const connection = { start: vi.fn() };

        await startConnection(connection as never, () => true);

        expect(connection.start).not.toHaveBeenCalled();
    });

    it('starts the connection when still active', async () => {
        const connection = { start: vi.fn().mockResolvedValue(undefined) };

        await startConnection(connection as never, () => false);

        expect(connection.start).toHaveBeenCalledTimes(1);
        expect(console.log).toHaveBeenCalledWith('[SignalR] Connected successfully');
    });

    it('retries failed connections after five seconds', async () => {
        const connection = {
            start: vi.fn().mockRejectedValueOnce(new Error('offline')).mockResolvedValue(undefined),
        };

        await startConnection(connection as never, () => false);
        expect(connection.start).toHaveBeenCalledTimes(1);

        await vi.advanceTimersByTimeAsync(5_000);
        expect(connection.start).toHaveBeenCalledTimes(2);
    });

    it('skips success logging when disposed during startup', async () => {
        let disposed = false;
        const connection = {
            start: vi.fn().mockImplementation(async () => {
                disposed = true;
            }),
        };

        await startConnection(connection as never, () => disposed);

        expect(console.log).not.toHaveBeenCalled();
    });

    it('does not retry when disposed after a failed start', async () => {
        const connection = {
            start: vi.fn().mockRejectedValue(new Error('offline')),
        };
        const isDisposed = vi.fn().mockReturnValueOnce(false).mockReturnValueOnce(true);

        await startConnection(connection as never, isDisposed);

        await vi.advanceTimersByTimeAsync(5_000);
        expect(connection.start).toHaveBeenCalledTimes(1);
    });
});
