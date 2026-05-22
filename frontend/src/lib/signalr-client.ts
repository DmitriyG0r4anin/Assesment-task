import {
  HubConnectionBuilder,
  HubConnection,
  LogLevel,
  HttpTransportType,
} from "@microsoft/signalr";
import config from "../types/config";

export function createSignalRConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(config.notificationsUrl, {
      skipNegotiation: true,
      transport: HttpTransportType.WebSockets,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(
      import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning,
    )
    .build();
}

export async function startConnection(
  connection: HubConnection,
  isDisposed: () => boolean,
): Promise<void> {
  if (isDisposed()) return;

  try {
    await connection.start();
    if (!isDisposed()) {
      console.log("[SignalR] Connected successfully");
    }
  } catch (err) {
    if (isDisposed()) return;
    console.error("[SignalR] Connection failed:", err);
    window.setTimeout(() => {
      void startConnection(connection, isDisposed);
    }, 5000);
  }
}
