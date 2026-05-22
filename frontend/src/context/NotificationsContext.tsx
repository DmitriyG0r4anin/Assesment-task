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
  clearMotionEvents,
  pushMotionEvent,
} from "../lib/motion-events";
import { formatTimestamp, parseTimestamp } from "../lib/format";
import {
  createSignalRConnection,
  startConnection,
} from "../lib/signalr-client";
import type { MotionEvent } from "../types";

export type ConnectionStatus = "disconnected" | "connecting" | "connected";

interface NotificationsContextValue {
  connectionStatus: ConnectionStatus;
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
  if (timestamp) {
    timestamp = parseTimestamp(timestamp).toISOString();
  } else {
    timestamp = new Date().toISOString();
  }
  return { roomName, isDetected, timestamp };
}

function handleMotionEvent(event: MotionEvent): void {
  pushMotionEvent(event);
  toast.info(
    event.isDetected
      ? `Motion: ${event.roomName}`
      : `Clear: ${event.roomName}`,
    {
      description: formatTimestamp(event.timestamp),
    },
  );
}

export function NotificationsProvider({ children }: { children: ReactNode }) {
  const [connectionStatus, setConnectionStatus] =
    useState<ConnectionStatus>("disconnected");
  const connectionRef = useRef<HubConnection | null>(null);
  const disposedRef = useRef(false);

  useEffect(() => {
    disposedRef.current = false;
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
    void startConnection(connection, () => disposedRef.current).then(() => {
      if (
        !disposedRef.current &&
        connection.state === HubConnectionState.Connected
      ) {
        setConnectionStatus("connected");
      }
    });

    return () => {
      disposedRef.current = true;
      void connection.stop();
    };
  }, []);

  const reconnect = useCallback(() => {
    const connection = connectionRef.current;
    if (!connection) return;
    setConnectionStatus("connecting");
    void startConnection(connection, () => disposedRef.current).then(() => {
      if (
        !disposedRef.current &&
        connection.state === HubConnectionState.Connected
      ) {
        setConnectionStatus("connected");
      }
    });
  }, []);

  const value = useMemo(
    () => ({
      connectionStatus,
      reconnect,
    }),
    [connectionStatus, reconnect],
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
    throw new Error(
      "useNotifications must be used within NotificationsProvider",
    );
  }
  return ctx;
}

export { clearMotionEvents };
