import {
    appendNewById,
    buildRoomNameMap,
    filterByTimeRange,
    formatRefreshInterval,
    getRefreshIntervalInputStep,
    getTimeRangeBounds,
    isWithinTimeRange,
    normalizeRefreshIntervalInput,
    refreshIntervalToMs,
    resolveRefreshIntervalMs,
    resolveRoomName,
    sortByTimestampAsc,
    sortByTimestampDesc,
} from '@/lib/metrics';

describe('sortByTimestampDesc', () => {
    it('orders items from newest to oldest', () => {
        const items = [
            { id: '1', timestamp: '2024-01-01T10:00:00Z' },
            { id: '2', timestamp: '2024-01-01T12:00:00Z' },
            { id: '3', timestamp: '2024-01-01T11:00:00Z' },
        ];

        expect(sortByTimestampDesc(items).map((item) => item.id)).toEqual(['2', '3', '1']);
    });
});

describe('sortByTimestampAsc', () => {
    it('orders items from oldest to newest', () => {
        const items = [
            { id: '1', timestamp: '2024-01-01T12:00:00Z' },
            { id: '2', timestamp: '2024-01-01T10:00:00Z' },
        ];

        expect(sortByTimestampAsc(items).map((item) => item.id)).toEqual(['2', '1']);
    });
});

describe('room name helpers', () => {
    it('builds a trimmed room name map with id fallback', () => {
        const map = buildRoomNameMap([
            { id: 'room-1', name: ' Kitchen ' },
            { id: 'room-2', name: '   ' },
        ]);

        expect(map.get('room-1')).toBe('Kitchen');
        expect(map.get('room-2')).toBe('room-2');
    });

    it('resolves unknown room ids to the id itself', () => {
        const map = buildRoomNameMap([{ id: 'room-1', name: 'Kitchen' }]);
        expect(resolveRoomName(map, 'missing')).toBe('missing');
    });
});

describe('time range helpers', () => {
    const now = new Date('2024-06-01T12:00:00.000Z');

    it('returns only endTime when range is open-ended', () => {
        expect(getTimeRangeBounds(null, now)).toEqual({
            endTime: '2024-06-01T12:00:00.000Z',
        });
    });

    it('returns start and end times for bounded ranges', () => {
        expect(getTimeRangeBounds(15, now)).toEqual({
            startTime: '2024-06-01T11:45:00.000Z',
            endTime: '2024-06-01T12:00:00.000Z',
        });
    });

    it('accepts all timestamps when no range is selected', () => {
        expect(isWithinTimeRange('2024-01-01T00:00:00Z', null, now)).toBe(true);
    });

    it('filters items outside the selected range', () => {
        const items = [
            { id: '1', timestamp: '2024-06-01T11:50:00.000Z' },
            { id: '2', timestamp: '2024-05-01T11:50:00.000Z' },
        ];

        expect(filterByTimeRange(items, 15, now).map((item) => item.id)).toEqual(['1']);
    });
});

describe('refresh interval helpers', () => {
    it('converts refresh units to milliseconds', () => {
        expect(refreshIntervalToMs(2, 'seconds')).toBe(2_000);
        expect(refreshIntervalToMs(2, 'minutes')).toBe(120_000);
        expect(refreshIntervalToMs(2, 'hours')).toBe(7_200_000);
    });

    it('defaults invalid refresh intervals to one second', () => {
        expect(resolveRefreshIntervalMs(0, 'seconds')).toBe(1_000);
    });

    it('formats refresh intervals for display', () => {
        expect(formatRefreshInterval(1_000)).toBe('1 second');
        expect(formatRefreshInterval(2_000)).toBe('2 seconds');
        expect(formatRefreshInterval(60_000)).toBe('1 minute');
        expect(formatRefreshInterval(3_600_000)).toBe('1 hour');
    });

    it('returns the correct input step per unit', () => {
        expect(getRefreshIntervalInputStep('hours')).toBe(0.01);
        expect(getRefreshIntervalInputStep('minutes')).toBe(1);
    });

    it('normalizes refresh input across unit boundaries', () => {
        expect(normalizeRefreshIntervalInput(60, 'seconds')).toEqual({
            value: 1,
            unit: 'minutes',
        });
        expect(normalizeRefreshIntervalInput(0, 'minutes')).toEqual({
            value: 59,
            unit: 'seconds',
        });
        expect(normalizeRefreshIntervalInput(60, 'hours')).toEqual({
            value: 59,
            unit: 'hours',
        });
        expect(normalizeRefreshIntervalInput(0, 'seconds')).toEqual({
            value: 1,
            unit: 'seconds',
        });
        expect(normalizeRefreshIntervalInput(Number.NaN, 'hours')).toEqual({
            value: 1,
            unit: 'hours',
        });
    });
});

describe('appendNewById', () => {
    it('appends only unseen rows and keeps newest first', () => {
        const existing = [
            { id: '1', timestamp: '2024-01-01T12:00:00Z' },
            { id: '2', timestamp: '2024-01-01T11:00:00Z' },
        ];
        const incoming = [
            { id: '2', timestamp: '2024-01-01T11:30:00Z' },
            { id: '3', timestamp: '2024-01-01T13:00:00Z' },
        ];

        expect(appendNewById(existing, incoming, 10).map((row) => row.id)).toEqual(['3', '1', '2']);
    });

    it('returns the existing list when there is nothing new', () => {
        const existing = [{ id: '1', timestamp: '2024-01-01T12:00:00Z' }];
        expect(appendNewById(existing, [], 10)).toBe(existing);
    });
});
