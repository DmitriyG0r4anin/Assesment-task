import { useCallback, useEffect, useMemo, useState } from "react";
import { ROOMS_QUERY, AIR_QUERY, ENERGY_QUERY, MOTIONS_QUERY } from "../types/graphql";
import { graphqlClient } from "../lib/graphql-client";
import { resolveRoomName, sortByTimestampDesc } from "../lib/metrics";
import type {
  AirQualitiesQueryResponse,
  AirQuality,
  EnergiesQueryResponse,
  Energy,
  Motion,
  MotionFilter,
  MotionsQueryResponse,
  RoomsQueryResponse,
  SensorFilter,
} from "../types";

const PAGE_SIZE = 15;

type Tab = "air" | "energy" | "motion";

function formatTimestamp(ts: string): string {
  try {
    return new Date(ts).toLocaleString();
  } catch {
    return ts;
  }
}

function toIsoOrUndefined(value: string): string | undefined {
  if (!value) return undefined;
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return undefined;
  return d.toISOString();
}

export function Parameters() {
  const [tab, setTab] = useState<Tab>("air");
  const [roomOptions, setRoomOptions] = useState<{ id: string; name: string }[]>(
    [],
  );

  const [draftRoomId, setDraftRoomId] = useState("");
  const [draftStart, setDraftStart] = useState("");
  const [draftEnd, setDraftEnd] = useState("");
  const [draftDetected, setDraftDetected] = useState("");

  const [appliedRoomId, setAppliedRoomId] = useState("");
  const [appliedStart, setAppliedStart] = useState("");
  const [appliedEnd, setAppliedEnd] = useState("");
  const [appliedDetected, setAppliedDetected] = useState("");

  const [airRows, setAirRows] = useState<AirQuality[]>([]);
  const [energyRows, setEnergyRows] = useState<Energy[]>([]);
  const [motionRows, setMotionRows] = useState<Motion[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [hasNextPage, setHasNextPage] = useState(false);
  const [offset, setOffset] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function loadRooms() {
      try {
        const data = await graphqlClient.request<RoomsQueryResponse>(
          ROOMS_QUERY,
          { pagination: { offset: 0, limit: 500 } },
        );
        setRoomOptions(data.rooms.items);
      } catch (e) {
        console.error(e);
      }
    }
    void loadRooms();
  }, []);

  const buildSensorFilter = useCallback((): SensorFilter | undefined => {
    const timestampStart = toIsoOrUndefined(appliedStart);
    const timestampEnd = toIsoOrUndefined(appliedEnd);
    const roomId = appliedRoomId || undefined;
    if (!timestampStart && !timestampEnd && !roomId) return undefined;
    return { timestampStart, timestampEnd, roomId };
  }, [appliedRoomId, appliedStart, appliedEnd]);

  const buildMotionFilter = useCallback((): MotionFilter | undefined => {
    const base = buildSensorFilter() ?? {};
    const motion: MotionFilter = { ...base };
    if (appliedDetected === "true") motion.isDetected = true;
    if (appliedDetected === "false") motion.isDetected = false;
    if (
      !motion.timestampStart &&
      !motion.timestampEnd &&
      !motion.roomId &&
      motion.isDetected === undefined
    ) {
      return undefined;
    }
    return motion;
  }, [buildSensorFilter, appliedDetected]);

  const fetchPage = useCallback(async () => {
    setLoading(true);
    setError(null);
    const pagination = { offset, limit: PAGE_SIZE };

    try {
      switch (tab) {
        case "air": {
          const filter = buildSensorFilter();
          const data = await graphqlClient.request<AirQualitiesQueryResponse>(
            AIR_QUERY,
            { filter, pagination },
          );
          setAirRows(sortByTimestampDesc(data.airQualities.items));
          setTotalCount(data.airQualities.totalCount);
          setHasNextPage(data.airQualities.hasNextPage);
          break;
        }
        case "energy": {
          const filter = buildSensorFilter();
          const data = await graphqlClient.request<EnergiesQueryResponse>(
            ENERGY_QUERY,
            { filter, pagination },
          );
          setEnergyRows(sortByTimestampDesc(data.energies.items));
          setTotalCount(data.energies.totalCount);
          setHasNextPage(data.energies.hasNextPage);
          break;
        }
        case "motion": {
          const filter = buildMotionFilter();
          const data = await graphqlClient.request<MotionsQueryResponse>(
            MOTIONS_QUERY,
            { filter, pagination },
          );
          setMotionRows(sortByTimestampDesc(data.motions.items));
          setTotalCount(data.motions.totalCount);
          setHasNextPage(data.motions.hasNextPage);
          break;
        }
      }
    } catch (err) {
      console.error(err);
      setError(
        err instanceof Error ? err.message : "Failed to load sensor data",
      );
    } finally {
      setLoading(false);
    }
  }, [tab, offset, buildSensorFilter, buildMotionFilter]);

  useEffect(() => {
    void fetchPage();
  }, [fetchPage]);

  const handleApply = () => {
    setAppliedRoomId(draftRoomId);
    setAppliedStart(draftStart);
    setAppliedEnd(draftEnd);
    setAppliedDetected(draftDetected);
    setOffset(0);
  };

  const handleReset = () => {
    setDraftRoomId("");
    setDraftStart("");
    setDraftEnd("");
    setDraftDetected("");
    setAppliedRoomId("");
    setAppliedStart("");
    setAppliedEnd("");
    setAppliedDetected("");
    setOffset(0);
  };

  const selectTab = (id: Tab) => {
    setTab(id);
    setOffset(0);
  };

  const roomNameById = useMemo(
    () => new Map(roomOptions.map((r) => [r.id, r.name.trim() || r.id])),
    [roomOptions],
  );

  const currentPage = Math.floor(offset / PAGE_SIZE) + 1;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  const tabBtn = (id: Tab, label: string) => (
    <button
      type="button"
      key={id}
      onClick={() => selectTab(id)}
      className={`rounded-lg px-4 py-2 text-sm font-medium transition ${
        tab === id
          ? "bg-blue-600 text-white shadow"
          : "bg-slate-100 text-slate-700 hover:bg-slate-200"
      }`}
    >
      {label}
    </button>
  );

  return (
    <div className="space-y-8">
      <header>
        <h1 className="text-2xl font-bold tracking-tight text-slate-900">
          Sensor data
        </h1>
        <p className="mt-1 text-slate-600">
          Browse air quality, energy, and motion records from the GraphQL API
        </p>
      </header>

      <div className="flex flex-wrap gap-2">
        {tabBtn("air", "Air quality")}
        {tabBtn("energy", "Energy")}
        {tabBtn("motion", "Motion")}
      </div>

      <section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 pb-4">
          <h2 className="font-semibold text-slate-800">Filters</h2>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={handleApply}
              className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow hover:bg-blue-700"
            >
              Apply
            </button>
            <button
              type="button"
              onClick={handleReset}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
            >
              Reset
            </button>
          </div>
        </div>

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <label className="flex flex-col gap-1 text-sm text-slate-600">
            Room
            <select
              className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-slate-900 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/20"
              value={draftRoomId}
              onChange={(e) => setDraftRoomId(e.target.value)}
            >
              <option value="">All rooms</option>
              {roomOptions.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.name} ({r.id.slice(0, 8)}…)
                </option>
              ))}
            </select>
          </label>
          <label className="flex flex-col gap-1 text-sm text-slate-600">
            Start time
            <input
              type="datetime-local"
              className="rounded-lg border border-slate-300 px-3 py-2 text-slate-900 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/20"
              value={draftStart}
              onChange={(e) => setDraftStart(e.target.value)}
            />
          </label>
          <label className="flex flex-col gap-1 text-sm text-slate-600">
            End time
            <input
              type="datetime-local"
              className="rounded-lg border border-slate-300 px-3 py-2 text-slate-900 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/20"
              value={draftEnd}
              onChange={(e) => setDraftEnd(e.target.value)}
            />
          </label>
          {tab === "motion" && (
            <label className="flex flex-col gap-1 text-sm text-slate-600">
              Detected
              <select
                className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-slate-900 focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/20"
                value={draftDetected}
                onChange={(e) => setDraftDetected(e.target.value)}
              >
                <option value="">Any</option>
                <option value="true">Yes</option>
                <option value="false">No</option>
              </select>
            </label>
          )}
        </div>
      </section>

      {error && (
        <div
          className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"
          role="alert"
        >
          {error}
        </div>
      )}

      <section className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
        <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
          <h2 className="font-semibold text-slate-800">
            Results{" "}
            <span className="font-normal text-slate-500">({totalCount})</span>
          </h2>
        </div>

        {loading ? (
          <div className="flex items-center justify-center gap-3 py-20 text-slate-500">
            <span
              className="size-5 animate-spin rounded-full border-2 border-slate-200 border-t-blue-600"
              aria-hidden
            />
            Loading…
          </div>
        ) : tab === "air" && airRows.length === 0 ? (
          <p className="p-10 text-center text-slate-500">No air quality rows.</p>
        ) : tab === "energy" && energyRows.length === 0 ? (
          <p className="p-10 text-center text-slate-500">No energy rows.</p>
        ) : tab === "motion" && motionRows.length === 0 ? (
          <p className="p-10 text-center text-slate-500">No motion rows.</p>
        ) : (
          <>
            <div className="overflow-x-auto">
              {tab === "air" && (
                <table className="w-full min-w-[640px] text-left text-sm">
                  <thead className="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase tracking-wide text-slate-500">
                    <tr>
                      <th className="px-4 py-3">Room</th>
                      <th className="px-4 py-3">Time</th>
                      <th className="px-4 py-3">PM2.5</th>
                      <th className="px-4 py-3">CO₂</th>
                      <th className="px-4 py-3">Humidity</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {airRows.map((row) => (
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
              )}
              {tab === "energy" && (
                <table className="w-full min-w-[520px] text-left text-sm">
                  <thead className="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase tracking-wide text-slate-500">
                    <tr>
                      <th className="px-4 py-3">Room</th>
                      <th className="px-4 py-3">Time</th>
                      <th className="px-4 py-3">Amount (kWh)</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {energyRows.map((row) => (
                      <tr key={row.id} className="hover:bg-slate-50/80">
                        <td className="px-4 py-3 text-slate-800">
                          {resolveRoomName(roomNameById, row.roomId)}
                        </td>
                        <td className="px-4 py-3 text-slate-600">
                          {formatTimestamp(row.timestamp)}
                        </td>
                        <td className="px-4 py-3">{row.amount.toFixed(3)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
              {tab === "motion" && (
                <table className="w-full min-w-[520px] text-left text-sm">
                  <thead className="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase tracking-wide text-slate-500">
                    <tr>
                      <th className="px-4 py-3">Room</th>
                      <th className="px-4 py-3">Time</th>
                      <th className="px-4 py-3">Detected</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {motionRows.map((row) => (
                      <tr key={row.id} className="hover:bg-slate-50/80">
                        <td className="px-4 py-3 text-slate-800">
                          {resolveRoomName(roomNameById, row.roomId)}
                        </td>
                        <td className="px-4 py-3 text-slate-600">
                          {formatTimestamp(row.timestamp)}
                        </td>
                        <td className="px-4 py-3">
                          <span
                            className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${
                              row.isDetected
                                ? "bg-emerald-100 text-emerald-800"
                                : "bg-slate-100 text-slate-600"
                            }`}
                          >
                            {row.isDetected ? "Yes" : "No"}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>

            <div className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-100 px-5 py-4">
              <p className="text-sm text-slate-600">
                Page {currentPage} of {totalPages} · Showing {offset + 1}–
                {Math.min(offset + PAGE_SIZE, totalCount)} of {totalCount}
              </p>
              <div className="flex gap-2">
                <button
                  type="button"
                  disabled={offset === 0}
                  onClick={() => setOffset((o) => Math.max(0, o - PAGE_SIZE))}
                  className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
                >
                  Previous
                </button>
                <button
                  type="button"
                  disabled={!hasNextPage}
                  onClick={() => setOffset((o) => o + PAGE_SIZE)}
                  className="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
                >
                  Next
                </button>
              </div>
            </div>
          </>
        )}
      </section>
    </div>
  );
}

export default Parameters;
