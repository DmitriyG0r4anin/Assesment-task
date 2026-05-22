import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { HubConnection, HubConnectionState } from "@microsoft/signalr";
import { toast } from "sonner";
import {
  createSignalRConnection,
  startConnection,
} from "../lib/signalr-client";
import type { MotionEvent } from "../types";

type ConnectionStatus = "disconnected" | "connecting" | "connected";

const MAX_EVENTS = 100;

interface NotificationsContextValue {
  connectionStatus: ConnectionStatus;
  events: MotionEvent[];
  clearEvents: () => void;
  reconnect: () => void;
}

const NotificationsContext = createContext<NotificationsContextValue | null>(
  null,
);

function parseMotionPayload(data: unknown): MotionEvent | null {
  if (!data || typeof data !== "object") return null;
  const raw = data as Record<string, unknown>;
  const roomName =
    (raw.roomName as string) ?? (raw.RoomName as string) ?? undefined;
  if (!roomName) return null;
  const isDetected =
    (raw.isDetected as boolean) ?? (raw.IsDetected as boolean) ?? false;
  let timestamp =
    (raw.timestamp as string) ?? (raw.Timestamp as string) ?? undefined;
  if (typeof timestamp === "string" && !timestamp.includes("T")) {
    timestamp = new Date(timestamp).toISOString();
  }
  if (!timestamp) timestamp = new Date().toISOString();
  return { roomName, isDetected, timestamp };
}

export function NotificationsProvider({ children }: { children: ReactNode }) {
  const [events, setEvents] = useState<MotionEvent[]>([]);
  const [connectionStatus, setConnectionStatus] =
    useState<ConnectionStatus>("disconnected");
  const connectionRef = useRef<HubConnection | null>(null);

  const handleMotionEvent = useCallback((event: MotionEvent) => {
    setEvents((prev) => {
      const next = [event, ...prev];
      return next.length > MAX_EVENTS ? next.slice(0, MAX_EVENTS) : next;
    });

    toast.info(
      event.isDetected
        ? `Motion: ${event.roomName}`
        : `Clear: ${event.roomName}`,
      {
        description: new Date(event.timestamp).toLocaleString(),
      },
    );
  }, []);

  useEffect(() => {
    const connection = createSignalRConnection();
    connectionRef.current = connection;

    connection.onreconnecting(() => {
      setConnectionStatus("connecting");
    });

    connection.onreconnected(() => {
      setConnectionStatus("connected");
    });

    connection.onclose(() => {
      setConnectionStatus("disconnected");
    });

    connection.on("MotionDetected", (data: unknown) => {
      const event = parseMotionPayload(data);
      if (event) handleMotionEvent(event);
    });

    setConnectionStatus("connecting");
    void startConnection(connection).then(() => {
      if (connection.state === HubConnectionState.Connected) {
        setConnectionStatus("connected");
      }
    });

    return () => {
      void connection.stop();
    };
  }, [handleMotionEvent]);

  const clearEvents = useCallback(() => {
    setEvents([]);
  }, []);

  const reconnect = useCallback(() => {
    const connection = connectionRef.current;
    if (!connection) return;
    setConnectionStatus("connecting");
    void startConnection(connection).then(() => {
      if (connection.state === HubConnectionState.Connected) {
        setConnectionStatus("connected");
      }
    });
  }, []);

  const value = useMemo(
    () => ({
      connectionStatus,
      events,
      clearEvents,
      reconnect,
    }),
    [connectionStatus, events, clearEvents, reconnect],
  );

  return (
    <NotificationsContext.Provider value={value}>
      {children}
    </NotificationsContext.Provider>
  );
}

export function useNotifications(): NotificationsContextValue {
  const ctx = useContext(NotificationsContext);
  if (!ctx) {
    throw new Error("useNotifications must be used within NotificationsProvider");
  }
  return ctx;
}
