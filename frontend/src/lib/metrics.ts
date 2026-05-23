function timestampMs(ts: string): number {
    const ms = new Date(ts).getTime();
    return Number.isNaN(ms) ? 0 : ms;
}

/** Newest first (top → bottom in tables) */
export function sortByTimestampDesc<T extends { timestamp: string }>(items: T[]): T[] {
    return [...items].sort((a, b) => timestampMs(b.timestamp) - timestampMs(a.timestamp));
}

/** Oldest first (left → right on time-series charts) */
export function sortByTimestampAsc<T extends { timestamp: string }>(items: T[]): T[] {
    return [...items].sort((a, b) => timestampMs(a.timestamp) - timestampMs(b.timestamp));
}

export function buildRoomNameMap(rooms: ReadonlyArray<{ id: string; name: string }>): Map<string, string> {
    return new Map(rooms.map((r) => [r.id, r.name.trim() || r.id]));
}

export function resolveRoomName(map: ReadonlyMap<string, string>, roomId: string): string {
    return map.get(roomId) ?? roomId;
}

/** Keep newest items; on poll only adds rows whose id is not already present. */
export type ChartTimeRangeMinutes = number | null;

export function getTimeRangeBounds(
    rangeMinutes: ChartTimeRangeMinutes,
    now: Date = new Date(),
): { startTime?: string; endTime: string } {
    const endTime = now.toISOString();
    if (rangeMinutes == null || rangeMinutes <= 0) {
        return { endTime };
    }
    const start = new Date(now.getTime() - rangeMinutes * 60_000);
    return { startTime: start.toISOString(), endTime };
}

export function isWithinTimeRange(
    timestamp: string,
    rangeMinutes: ChartTimeRangeMinutes,
    now: Date = new Date(),
): boolean {
    if (rangeMinutes == null || rangeMinutes <= 0) return true;
    const ms = timestampMs(timestamp);
    if (ms === 0) return false;
    const start = now.getTime() - rangeMinutes * 60_000;
    return ms >= start && ms <= now.getTime();
}

export function filterByTimeRange<T extends { timestamp: string }>(
    items: T[],
    rangeMinutes: ChartTimeRangeMinutes,
    now?: Date,
): T[] {
    if (rangeMinutes == null || rangeMinutes <= 0) return items;
    return items.filter((item) => isWithinTimeRange(item.timestamp, rangeMinutes, now));
}

export type RefreshIntervalUnit = 'seconds' | 'minutes' | 'hours';

export function refreshIntervalToMs(value: number, unit: RefreshIntervalUnit): number {
    const n = Math.max(0, value);
    switch (unit) {
        case 'seconds':
            return n * 1_000;
        case 'minutes':
            return n * 60_000;
        case 'hours':
            return n * 3_600_000;
    }
}

/** Converts user input to a positive poll interval (defaults to 1s if invalid). */
export function resolveRefreshIntervalMs(value: number, unit: RefreshIntervalUnit): number {
    const ms = refreshIntervalToMs(value, unit);
    return ms > 0 ? ms : 1_000;
}

export function formatRefreshInterval(ms: number): string {
    if (ms < 60_000) {
        const s = ms / 1_000;
        return `${s} second${s === 1 ? '' : 's'}`;
    }
    if (ms < 3_600_000) {
        const m = ms / 60_000;
        const label = Number.isInteger(m) ? String(m) : m.toFixed(1);
        return `${label} minute${m === 1 ? '' : 's'}`;
    }
    const h = ms / 3_600_000;
    const label = Number.isInteger(h) ? String(h) : h.toFixed(2);
    return `${label} hour${h === 1 ? '' : 's'}`;
}

export function getRefreshIntervalInputStep(unit: RefreshIntervalUnit): number {
    return unit === 'hours' ? 0.01 : 1;
}

const LARGER_REFRESH_UNIT: Record<RefreshIntervalUnit, RefreshIntervalUnit | null> = {
    seconds: 'minutes',
    minutes: 'hours',
    hours: null,
};

const SMALLER_REFRESH_UNIT: Record<RefreshIntervalUnit, RefreshIntervalUnit | null> = {
    seconds: null,
    minutes: 'seconds',
    hours: 'minutes',
};

/**
 * Keeps refresh interval in 1–59 per unit; rolls up/down when crossing bounds.
 * Below 1 → smaller unit at 59; above 59 → larger unit at 1.
 */
export function normalizeRefreshIntervalInput(
    value: number,
    unit: RefreshIntervalUnit,
): { value: number; unit: RefreshIntervalUnit } {
    if (!Number.isFinite(value)) {
        return { value: 1, unit };
    }

    if (value > 59) {
        const larger = LARGER_REFRESH_UNIT[unit];
        if (larger) return { value: 1, unit: larger };
        return { value: 59, unit };
    }

    if (value < 1) {
        const smaller = SMALLER_REFRESH_UNIT[unit];
        if (smaller) return { value: 59, unit: smaller };
        return { value: 1, unit };
    }

    return { value, unit };
}

export function appendNewById<T extends { id: string; timestamp: string }>(
    existing: T[],
    incoming: T[],
    maxItems: number,
): T[] {
    if (incoming.length === 0) return existing;
    const seen = new Set(existing.map((row) => row.id));
    const added = incoming.filter((row) => !seen.has(row.id));
    if (added.length === 0) return existing;
    return sortByTimestampDesc([...added, ...existing]).slice(0, maxItems);
}
