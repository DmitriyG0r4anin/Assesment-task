import { useMemo } from "react";
import { useNotifications } from "../context/NotificationsContext";
import type { MotionEvent } from "../types";

type ConnectionStatus = "disconnected" | "connecting" | "connected";

interface RoomStatus {
  roomName: string;
  isDetected: boolean;
  lastSeen: string;
}

function formatTime(iso: string): string {
  try {
    return new Date(iso).toLocaleTimeString(undefined, {
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    });
  } catch {
    return iso;
  }
}

function formatDateTime(iso: string): string {
  try {
    return new Date(iso).toLocaleString(undefined, {
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    });
  } catch {
    return iso;
  }
}

function eventKey(event: MotionEvent, index: number): string {
  return `${event.roomName}-${event.timestamp}-${index}`;
}

export function Motion() {
  const { connectionStatus, events, clearEvents, reconnect } =
    useNotifications();

  const roomStatuses = useMemo(() => {
    const map = new Map<string, RoomStatus>();
    for (const e of events) {
      if (!map.has(e.roomName)) {
        map.set(e.roomName, {
          roomName: e.roomName,
          isDetected: e.isDetected,
          lastSeen: e.timestamp,
        });
      }
    }
    return map;
  }, [events]);

  const statusLabel: Record<ConnectionStatus, string> = {
    connected: "Connected",
    connecting: "Connecting…",
    disconnected: "Disconnected",
  };

  const sortedRooms = Array.from(roomStatuses.values()).sort((a, b) =>
    a.roomName.localeCompare(b.roomName),
  );

  const activeRoomCount = sortedRooms.filter((r) => r.isDetected).length;

  return (
    <div className="space-y-8">
      <header>
        <h1 className="text-2xl font-bold tracking-tight text-slate-900">
          Motion tracker
        </h1>
        <p className="mt-1 text-slate-600">
          Live feed from the notifications service (SignalR). Toasts appear app-wide on each event.
        </p>
      </header>

      <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-2 text-sm font-medium text-slate-700">
            <span
              className={`size-2.5 shrink-0 rounded-full ${
                connectionStatus === "connected"
                  ? "bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.6)]"
                  : connectionStatus === "connecting"
                    ? "animate-pulse bg-amber-500"
                    : "bg-red-500 shadow-[0_0_8px_rgba(239,68,68,0.5)]"
              }`}
              aria-hidden
            />
            {statusLabel[connectionStatus]}
          </div>
          <div className="flex flex-wrap items-center gap-3 text-sm text-slate-600">
            <span>
              {events.length} event{events.length !== 1 ? "s" : ""} buffered
            </span>
            {activeRoomCount > 0 && (
              <span className="rounded-full bg-emerald-100 px-2.5 py-0.5 text-xs font-medium text-emerald-800">
                {activeRoomCount} active
              </span>
            )}
            {connectionStatus === "disconnected" && (
              <button
                type="button"
                onClick={reconnect}
                className="rounded-lg bg-blue-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-blue-700"
              >
                Reconnect
              </button>
            )}
            {events.length > 0 && (
              <button
                type="button"
                onClick={clearEvents}
                className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
              >
                Clear
              </button>
            )}
          </div>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-4 flex items-center justify-between border-b border-slate-100 pb-3">
            <h2 className="font-semibold text-slate-800">Live events</h2>
            <span className="text-xs text-slate-500">
              Newest first (max 100)
            </span>
          </div>

          {events.length === 0 ? (
            <div className="rounded-lg border border-dashed border-slate-200 py-12 text-center text-slate-500">
              {connectionStatus === "connected"
                ? "Waiting for motion events…"
                : "Connect to start receiving events."}
            </div>
          ) : (
            <ul className="max-h-[520px] space-y-2 overflow-y-auto pr-1">
              {events.map((event, index) => (
                <li
                  key={eventKey(event, index)}
                  className={`flex items-center justify-between rounded-lg border px-3 py-3 text-sm transition ${
                    event.isDetected
                      ? "border-l-4 border-l-emerald-500 border-slate-200 bg-white"
                      : "border-l-4 border-l-slate-300 border-slate-200 bg-slate-50/50"
                  }`}
                >
                  <div>
                    <div className="font-semibold text-slate-900">
                      {event.roomName}
                    </div>
                    <div className="text-xs text-slate-500">
                      {formatTime(event.timestamp)}
                    </div>
                  </div>
                  <span
                    className={`rounded-full px-2.5 py-1 text-xs font-medium ${
                      event.isDetected
                        ? "bg-emerald-100 text-emerald-800"
                        : "bg-slate-200 text-slate-600"
                    }`}
                  >
                    {event.isDetected ? "Motion" : "Clear"}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-4 flex items-center justify-between border-b border-slate-100 pb-3">
            <h2 className="font-semibold text-slate-800">Room status</h2>
            <span className="text-xs text-slate-500">
              {sortedRooms.length} room{sortedRooms.length !== 1 ? "s" : ""}
            </span>
          </div>

          {sortedRooms.length === 0 ? (
            <div className="rounded-lg border border-dashed border-slate-200 py-12 text-center text-slate-500">
              No room data yet.
            </div>
          ) : (
            <div className="grid max-h-[520px] grid-cols-1 gap-3 overflow-y-auto sm:grid-cols-2">
              {sortedRooms.map((room) => (
                <div
                  key={room.roomName}
                  className={`rounded-lg border p-4 text-center transition ${
                    room.isDetected
                      ? "border-emerald-300 bg-emerald-50/60"
                      : "border-slate-200 bg-white"
                  }`}
                >
                  <div className="font-semibold text-slate-900">
                    {room.roomName}
                  </div>
                  <div className="mt-2">
                    <span
                      className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${
                        room.isDetected
                          ? "bg-emerald-100 text-emerald-800"
                          : "bg-slate-100 text-slate-600"
                      }`}
                    >
                      {room.isDetected ? "Active" : "Inactive"}
                    </span>
                  </div>
                  <div className="mt-2 text-xs text-slate-500">
                    Last: {formatDateTime(room.lastSeen)}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default Motion;
