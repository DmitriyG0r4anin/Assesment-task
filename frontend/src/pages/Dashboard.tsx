import { useCallback, useEffect, useMemo, useRef, useState, type ComponentType, type SVGProps } from 'react';
import {
    MetricTrendChart,
    SeriesTrendChart,
    type SeriesTrendPoint,
    type TrendPoint,
} from '../components/MetricTrendChart';
import { GridLayoutIcon, ListLayoutIcon, RefreshIcon } from '../assets/icons';
import {
    ChartsGridSkeleton,
    chartsLayoutClassName,
    ContentLoadingOverlay,
    DashboardBootstrapSkeleton,
    RoomCardsSkeleton,
    RoomPillsSkeleton,
    TableSkeleton,
    type ChartsLayout,
} from '../components/DashboardSkeleton';
import { useRooms } from '../hooks/useRooms';
import { graphqlClient } from '../lib/graphql-client';
import { formatChartTime, formatNumber, formatTimestamp, formatTime } from '../lib/format';
import {
    appendNewById,
    buildRoomNameMap,
    filterByTimeRange,
    formatRefreshInterval,
    getRefreshIntervalInputStep,
    getTimeRangeBounds,
    normalizeRefreshIntervalInput,
    resolveRefreshIntervalMs,
    resolveRoomName,
    sortByTimestampAsc,
    sortByTimestampDesc,
    type ChartTimeRangeMinutes,
    type RefreshIntervalUnit,
} from '../lib/metrics';
import type {
    AggregateByRoomResponse,
    AirQualitiesQueryResponse,
    AirQuality,
    EnergiesQueryResponse,
    Energy,
    RoomAggregation,
} from '../types';
import { AGGREGATE_BY_ROOM, AIR_QUERY, ENERGY_QUERY } from '../types/graphql';
import {
    AIR_METRIC_CHARTS,
    CHART_TIME_RANGES,
    DASHBOARD_LIMITS,
    ENERGY_CHART,
    METRIC_LABELS,
    METRIC_UNITS,
    REFRESH_INTERVAL_UNITS,
} from '../types/constants';

function ChartsLayoutToggle({
    layout,
    onChange,
}: Readonly<{
    layout: ChartsLayout;
    onChange: (layout: ChartsLayout) => void;
}>) {
    const options: {
        value: ChartsLayout;
        label: string;
        Icon: ComponentType<SVGProps<SVGSVGElement>>;
    }[] = [
        {
            value: 'grid',
            label: 'Grid',
            Icon: GridLayoutIcon,
        },
        {
            value: 'list',
            label: 'List',
            Icon: ListLayoutIcon,
        },
    ];

    return (
        <fieldset className="m-0 flex min-w-0 flex-col gap-1 border-0 p-0">
            <legend className="text-sm text-slate-600">Charts</legend>
            <div className="inline-flex rounded-lg border border-slate-300 bg-white p-0.5 shadow-sm">
                {options.map((option) => {
                    const active = layout === option.value;
                    return (
                        <button
                            key={option.value}
                            type="button"
                            onClick={() => onChange(option.value)}
                            aria-pressed={active}
                            title={`${option.label} layout`}
                            className={`inline-flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-sm font-medium transition ${
                                active ? 'bg-blue-600 text-white shadow-sm' : 'text-slate-600 hover:bg-slate-50'
                            }`}
                        >
                            <option.Icon className="ui-icon-sm" aria-hidden />
                            <span>{option.label}</span>
                        </button>
                    );
                })}
            </div>
        </fieldset>
    );
}

function roomLabel(r: RoomAggregation): string {
    return r.roomName?.trim() || r.roomId;
}

function airToTrendPoints(readings: AirQuality[]): TrendPoint[] {
    return sortByTimestampAsc(readings).map((row) => ({
        time: formatChartTime(row.timestamp),
        bucketKey: row.id,
        avgCo2: row.co2,
        avgPm25: row.pm25,
        avgHumidity: row.humidity,
        motionCount: 0,
    }));
}

function energyToTrendPoints(readings: Energy[]): SeriesTrendPoint[] {
    return sortByTimestampAsc(readings).map((row) => ({
        time: formatChartTime(row.timestamp),
        bucketKey: row.id,
        value: row.amount,
    }));
}

export function Dashboard() {
    const [aggregations, setAggregations] = useState<RoomAggregation[]>([]);
    const [recentAir, setRecentAir] = useState<AirQuality[]>([]);
    const recentEnergyRef = useRef<Energy[]>([]);
    const [airTotal, setAirTotal] = useState(0);
    const [chartRangeMinutes, setChartRangeMinutes] = useState<ChartTimeRangeMinutes>(5);
    const [refreshIntervalValue, setRefreshIntervalValue] = useState(5);
    const [refreshIntervalUnit, setRefreshIntervalUnit] = useState<RefreshIntervalUnit>('seconds');
    const [selectedRoomId, setSelectedRoomId] = useState<string | null>(null);
    const [hasLoadedOnce, setHasLoadedOnce] = useState(false);
    const [contentLoading, setContentLoading] = useState(false);
    const [refreshing, setRefreshing] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [lastUpdated, setLastUpdated] = useState<Date | null>(null);
    const rooms = useRooms();
    const [metricTrendData, setMetricTrendData] = useState<TrendPoint[]>([]);
    const [energyTrendData, setEnergyTrendData] = useState<SeriesTrendPoint[]>([]);
    const [chartsLayout, setChartsLayout] = useState<ChartsLayout>('grid');

    const roomNameById = useMemo(() => {
        const map = buildRoomNameMap(rooms);
        for (const r of aggregations) {
            if (r.roomName?.trim()) {
                map.set(r.roomId, r.roomName.trim());
            }
        }
        return map;
    }, [rooms, aggregations]);

    const fetchDashboard = useCallback(
        async (mode: 'replace' | 'append') => {
            try {
                const { startTime, endTime } = getTimeRangeBounds(chartRangeMinutes);

                const sensorFilter = {
                    timestampEnd: endTime,
                    ...(startTime ? { timestampStart: startTime } : {}),
                    ...(selectedRoomId ? { roomId: selectedRoomId } : {}),
                };

                const pagination = {
                    offset: 0,
                    limit: mode === 'replace' ? DASHBOARD_LIMITS.sensorFetch : DASHBOARD_LIMITS.sensorPoll,
                };

                const aggVariables = {
                    endTime,
                    ...(startTime ? { startTime } : {}),
                };

                const [aggResult, airResult, energyResult] = await Promise.all([
                    graphqlClient.request<AggregateByRoomResponse>(AGGREGATE_BY_ROOM, aggVariables),
                    graphqlClient.request<AirQualitiesQueryResponse>(AIR_QUERY, {
                        filter: sensorFilter,
                        pagination,
                    }),
                    graphqlClient.request<EnergiesQueryResponse>(ENERGY_QUERY, {
                        filter: sensorFilter,
                        pagination,
                    }),
                ]);

                const incomingAir = filterByTimeRange(
                    sortByTimestampDesc(airResult.airQualities.items),
                    chartRangeMinutes,
                );

                const incomingEnergy = filterByTimeRange(
                    sortByTimestampDesc(energyResult.energies.items),
                    chartRangeMinutes,
                );

                setAggregations(aggResult.aggregateByRoom);
                setAirTotal(airResult.airQualities.totalCount);
                setLastUpdated(new Date());

                const applyAirToCharts = (readings: AirQuality[]) => {
                    setMetricTrendData(airToTrendPoints(readings));
                };

                const applyEnergyToCharts = (readings: Energy[]) => {
                    setEnergyTrendData(energyToTrendPoints(readings));
                };

                if (mode === 'replace') {
                    setRecentAir(incomingAir);
                    recentEnergyRef.current = incomingEnergy;
                    applyAirToCharts(incomingAir);
                    applyEnergyToCharts(incomingEnergy);
                } else {
                    setRecentAir((prev) => {
                        const appended = appendNewById(prev, incomingAir, DASHBOARD_LIMITS.sensorFetch);
                        const merged = filterByTimeRange(appended, chartRangeMinutes);
                        if (appended !== prev) {
                            applyAirToCharts(merged);
                        }
                        return merged;
                    });
                    const energyAppended = appendNewById(
                        recentEnergyRef.current,
                        incomingEnergy,
                        DASHBOARD_LIMITS.sensorFetch,
                    );
                    const energyMerged = filterByTimeRange(energyAppended, chartRangeMinutes);
                    if (energyAppended !== recentEnergyRef.current) {
                        applyEnergyToCharts(energyMerged);
                        recentEnergyRef.current = energyMerged;
                    }
                }
            } catch (err) {
                console.error('Failed to fetch dashboard data:', err);
                setError(err instanceof Error ? err.message : 'Failed to load dashboard data');
            }
        },
        [selectedRoomId, chartRangeMinutes],
    );

    const runReplaceFetch = useCallback(async () => {
        setError(null);
        await fetchDashboard('replace');
        setHasLoadedOnce(true);
    }, [fetchDashboard]);

    const handleRefreshNow = useCallback(() => {
        if (!hasLoadedOnce) return;
        setContentLoading(true);
        void runReplaceFetch().finally(() => setContentLoading(false));
    }, [hasLoadedOnce, runReplaceFetch]);

    const refreshIntervalMs = useMemo(
        () => resolveRefreshIntervalMs(refreshIntervalValue, refreshIntervalUnit),
        [refreshIntervalValue, refreshIntervalUnit],
    );

    const refreshIntervalLabel = useMemo(() => formatRefreshInterval(refreshIntervalMs), [refreshIntervalMs]);

    const refreshIntervalStep = useMemo(() => getRefreshIntervalInputStep(refreshIntervalUnit), [refreshIntervalUnit]);

    useEffect(() => {
        setContentLoading(hasLoadedOnce);
        void runReplaceFetch().finally(() => setContentLoading(false));
    }, [fetchDashboard, runReplaceFetch]);

    useEffect(() => {
        if (!hasLoadedOnce) return;
        const timer = globalThis.setInterval(() => {
            setRefreshing(true);
            void fetchDashboard('append').finally(() => setRefreshing(false));
        }, refreshIntervalMs);
        return () => globalThis.clearInterval(timer);
    }, [fetchDashboard, refreshIntervalMs, hasLoadedOnce]);

    const tableRows = useMemo(
        () => sortByTimestampDesc(filterByTimeRange(recentAir, chartRangeMinutes)).slice(0, DASHBOARD_LIMITS.tableRows),
        [recentAir, chartRangeMinutes],
    );

    const chartRangeLabel = CHART_TIME_RANGES.find((r) => r.minutes === chartRangeMinutes)?.label ?? 'All time';

    const selectedRoom = useMemo(
        () => aggregations.find((r) => r.roomId === selectedRoomId) ?? null,
        [aggregations, selectedRoomId],
    );

    if (!hasLoadedOnce) {
        return <DashboardBootstrapSkeleton />;
    }

    if (error && metricTrendData.length === 0 && energyTrendData.length === 0 && aggregations.length === 0) {
        return (
            <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800" role="alert">
                {error}
            </div>
        );
    }

    return (
        <div className="min-w-0 w-full max-w-full space-y-10">
            <header className="flex flex-wrap items-start justify-between gap-4">
                <div>
                    <h1 className="text-2xl font-bold tracking-tight text-slate-900">Dashboard</h1>
                    <p className="mt-1 text-slate-600">
                        Live air-quality trends (refreshes every {refreshIntervalLabel})
                    </p>
                </div>
                <div className="flex flex-wrap items-center gap-2">
                    <button
                        type="button"
                        onClick={handleRefreshNow}
                        disabled={contentLoading}
                        className="inline-flex items-center gap-2 rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 shadow-sm transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50"
                    >
                        {refreshing || contentLoading ? (
                            <>
                                <span
                                    className="size-4 animate-spin rounded-full border-2 border-slate-300 border-t-blue-600"
                                    aria-hidden
                                />
                                <span>Refreshing…</span>
                            </>
                        ) : (
                            <>
                                <RefreshIcon className="ui-icon-sm" aria-hidden />
                                Refresh now
                            </>
                        )}
                    </button>
                    {lastUpdated && <span className="text-sm text-slate-500">Updated {formatTime(lastUpdated)}</span>}
                </div>
            </header>

            {error && (
                <div
                    className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900"
                    role="alert"
                >
                    {error}
                </div>
            )}

            <section className="min-w-0 space-y-4">
                <div className="flex flex-wrap items-end justify-between gap-4">
                    <div className="min-w-0">
                        <h2 className="text-lg font-semibold text-slate-800">
                            Air quality & energy trends
                            {selectedRoom ? (
                                <span className="ml-2 text-base font-normal text-blue-600">
                                    — {roomLabel(selectedRoom)}
                                </span>
                            ) : (
                                <span className="ml-2 text-base font-normal text-slate-500">— all rooms</span>
                            )}
                        </h2>
                        <p className="mt-1 text-sm text-slate-500">
                            Select a room and time range for {chartRangeLabel.toLowerCase()}
                        </p>
                    </div>
                    <div className="flex flex-wrap items-end gap-4">
                        <label className="flex flex-col gap-1 text-sm text-slate-600">
                            <span>Time range</span>
                            <select
                                className="ui-select-time"
                                value={chartRangeMinutes ?? ''}
                                onChange={(e) => {
                                    const raw = e.target.value;
                                    const nextRange: ChartTimeRangeMinutes = raw === '' ? null : Number(raw);
                                    setChartRangeMinutes(nextRange);
                                }}
                            >
                                {CHART_TIME_RANGES.map((range) => (
                                    <option key={range.label} value={range.minutes ?? ''}>
                                        {range.label}
                                    </option>
                                ))}
                            </select>
                        </label>
                        <ChartsLayoutToggle layout={chartsLayout} onChange={setChartsLayout} />
                        <div className="flex flex-col gap-1 text-sm text-slate-600">
                            <span>Update frequency</span>
                            <div className="flex flex-wrap items-center gap-2">
                                <input
                                    type="number"
                                    min={0}
                                    step={refreshIntervalStep}
                                    value={refreshIntervalValue}
                                    onChange={(e) => {
                                        const parsed = Number(e.target.value);
                                        if (!Number.isFinite(parsed)) return;
                                        const next = normalizeRefreshIntervalInput(parsed, refreshIntervalUnit);
                                        setRefreshIntervalValue(next.value);
                                        setRefreshIntervalUnit(next.unit);
                                    }}
                                    className="ui-input-narrow"
                                    aria-label="Refresh interval amount"
                                />
                                <select
                                    className="ui-select-unit"
                                    value={refreshIntervalUnit}
                                    onChange={(e) => {
                                        const unit = e.target.value as RefreshIntervalUnit;
                                        const next = normalizeRefreshIntervalInput(refreshIntervalValue, unit);
                                        setRefreshIntervalValue(next.value);
                                        setRefreshIntervalUnit(next.unit);
                                    }}
                                    aria-label="Refresh interval unit"
                                >
                                    {REFRESH_INTERVAL_UNITS.map((u) => (
                                        <option key={u.value} value={u.value}>
                                            {u.label}
                                        </option>
                                    ))}
                                </select>
                            </div>
                        </div>
                    </div>
                </div>

                <ContentLoadingOverlay
                    loading={contentLoading}
                    skeleton={<RoomPillsSkeleton />}
                    minHeight="ui-section-min-xs"
                >
                    <div className="flex flex-wrap gap-2">
                        <button
                            type="button"
                            onClick={() => setSelectedRoomId(null)}
                            className={`rounded-full px-4 py-2 text-sm font-medium transition ${
                                selectedRoomId === null
                                    ? 'bg-blue-600 text-white shadow'
                                    : 'border border-slate-300 bg-white text-slate-700 hover:bg-slate-50'
                            }`}
                        >
                            All rooms
                        </button>
                        {aggregations.map((room) => (
                            <button
                                key={room.roomId}
                                type="button"
                                onClick={() => setSelectedRoomId(room.roomId)}
                                className={`rounded-full px-4 py-2 text-sm font-medium transition ${
                                    selectedRoomId === room.roomId
                                        ? 'bg-blue-600 text-white shadow'
                                        : 'border border-slate-300 bg-white text-slate-700 hover:bg-slate-50'
                                }`}
                            >
                                {roomLabel(room)}
                            </button>
                        ))}
                    </div>
                </ContentLoadingOverlay>

                <ContentLoadingOverlay
                    loading={contentLoading}
                    skeleton={<ChartsGridSkeleton layout={chartsLayout} />}
                    minHeight="ui-section-min-xl"
                >
                    {metricTrendData.length === 0 && energyTrendData.length === 0 ? (
                        <p className="rounded-xl border border-dashed border-slate-200 bg-white p-8 text-center text-slate-500">
                            No sensor readings in {chartRangeLabel.toLowerCase()}
                            {selectedRoom ? ` for ${roomLabel(selectedRoom)}` : ''}. Ingest sensor data or widen the
                            time range.
                        </p>
                    ) : (
                        <div className={chartsLayoutClassName(chartsLayout)}>
                            {AIR_METRIC_CHARTS.map((chart) => (
                                <div key={chart.suffix} className="min-w-0">
                                    <MetricTrendChart
                                        title={chart.title}
                                        data={metricTrendData}
                                        dataKey={chart.dataKey}
                                        seriesName={chart.seriesName}
                                        stroke={chart.stroke}
                                        unit={chart.unit}
                                        layoutKey={`${chart.suffix}-${chartsLayout}`}
                                    />
                                </div>
                            ))}
                            <div className="min-w-0">
                                <SeriesTrendChart
                                    title={ENERGY_CHART.title}
                                    data={energyTrendData}
                                    seriesName={ENERGY_CHART.seriesName}
                                    stroke={ENERGY_CHART.stroke}
                                    unit={ENERGY_CHART.unit}
                                    layoutKey={`${ENERGY_CHART.suffix}-${chartsLayout}`}
                                    decimals={ENERGY_CHART.decimals}
                                />
                            </div>
                        </div>
                    )}
                </ContentLoadingOverlay>
            </section>

            <section className="min-w-0 space-y-4">
                <div>
                    <h2 className="text-lg font-semibold text-slate-800">Averages by room</h2>
                    <p className="mt-1 text-sm text-slate-500">Room averages for {chartRangeLabel.toLowerCase()}</p>
                </div>
                <ContentLoadingOverlay
                    loading={contentLoading}
                    skeleton={<RoomCardsSkeleton />}
                    minHeight="ui-section-min-lg"
                >
                    {aggregations.length === 0 ? (
                        <p className="rounded-xl border border-dashed border-slate-200 bg-white p-8 text-center text-slate-500">
                            No room aggregates in {chartRangeLabel.toLowerCase()}.
                        </p>
                    ) : (
                        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                            {aggregations.map((room) => (
                                <div
                                    key={room.roomId}
                                    className={`rounded-xl border bg-white p-4 shadow-sm transition ${
                                        selectedRoomId === room.roomId
                                            ? 'border-blue-400 ring-2 ring-blue-100'
                                            : 'border-slate-200'
                                    }`}
                                >
                                    <div className="flex items-start justify-between gap-2">
                                        <div className="font-semibold text-blue-700">{roomLabel(room)}</div>
                                        <button
                                            type="button"
                                            onClick={() => setSelectedRoomId(room.roomId)}
                                            className={`shrink-0 rounded-lg px-2.5 py-1 text-xs font-medium transition ${
                                                selectedRoomId === room.roomId
                                                    ? 'bg-blue-600 text-white'
                                                    : 'border border-slate-300 text-slate-600 hover:bg-slate-50'
                                            }`}
                                        >
                                            {selectedRoomId === room.roomId ? 'Selected' : 'Trends'}
                                        </button>
                                    </div>
                                    <dl className="mt-3 grid grid-cols-2 gap-2 text-sm">
                                        <div>
                                            <dt className="text-xs uppercase text-slate-500">
                                                Avg {METRIC_LABELS.co2}
                                            </dt>
                                            <dd className="font-medium">
                                                {formatNumber(room.avgCo2)}
                                                {METRIC_UNITS.co2}
                                            </dd>
                                        </div>
                                        <div>
                                            <dt className="text-xs uppercase text-slate-500">
                                                Avg {METRIC_LABELS.pm25}
                                            </dt>
                                            <dd className="font-medium">
                                                {formatNumber(room.avgPm25)}
                                                {METRIC_UNITS.pm25}
                                            </dd>
                                        </div>
                                        <div>
                                            <dt className="text-xs uppercase text-slate-500">
                                                {METRIC_LABELS.humidity}
                                            </dt>
                                            <dd className="font-medium">
                                                {formatNumber(room.avgHumidity)}
                                                {METRIC_UNITS.humidity}
                                            </dd>
                                        </div>
                                        <div>
                                            <dt className="text-xs uppercase text-slate-500">{METRIC_LABELS.energy}</dt>
                                            <dd className="font-medium">
                                                {formatNumber(room.avgEnergy, DASHBOARD_LIMITS.energyCardDecimals)}
                                                {METRIC_UNITS.energy}
                                            </dd>
                                        </div>
                                        <div>
                                            <dt className="text-xs uppercase text-slate-500">Motion</dt>
                                            <dd className="font-medium">{room.motionCount}</dd>
                                        </div>
                                        <div>
                                            <dt className="text-xs uppercase text-slate-500">Records</dt>
                                            <dd className="font-medium">{room.totalCount}</dd>
                                        </div>
                                    </dl>
                                </div>
                            ))}
                        </div>
                    )}
                </ContentLoadingOverlay>
            </section>

            <section className="space-y-4">
                <div className="flex items-center justify-between gap-2">
                    <h2 className="text-lg font-semibold text-slate-800">
                        Recent air quality
                        {selectedRoom ? ` — ${roomLabel(selectedRoom)}` : ''}
                    </h2>
                    <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-600">
                        {airTotal} total
                    </span>
                </div>
                <ContentLoadingOverlay
                    loading={contentLoading}
                    skeleton={<TableSkeleton />}
                    minHeight="ui-section-min-md"
                >
                    <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
                        {tableRows.length === 0 ? (
                            <p className="p-8 text-center text-slate-500">No air quality rows yet.</p>
                        ) : (
                            <div className="overflow-x-auto">
                                <table className="ui-data-table-air">
                                    <thead className="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase tracking-wide text-slate-500">
                                        <tr>
                                            <th className="px-4 py-3">Room</th>
                                            <th className="px-4 py-3">Time</th>
                                            <th className="px-4 py-3">{METRIC_LABELS.pm25}</th>
                                            <th className="px-4 py-3">{METRIC_LABELS.co2}</th>
                                            <th className="px-4 py-3">{METRIC_LABELS.humidity}</th>
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y divide-slate-100">
                                        {tableRows.map((row) => (
                                            <tr key={row.id} className="hover:bg-slate-50/80">
                                                <td className="px-4 py-3 text-slate-800">
                                                    {resolveRoomName(roomNameById, row.roomId)}
                                                </td>
                                                <td className="px-4 py-3 text-slate-600">
                                                    {formatTimestamp(row.timestamp)}
                                                </td>
                                                <td className="px-4 py-3">{row.pm25}</td>
                                                <td className="px-4 py-3">{row.co2}</td>
                                                <td className="px-4 py-3">{row.humidity}%</td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        )}
                    </div>
                </ContentLoadingOverlay>
            </section>
        </div>
    );
}
