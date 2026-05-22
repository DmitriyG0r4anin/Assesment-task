import {
  HubConnectionBuilder,
  HubConnection,
  LogLevel,
  HttpTransportType,
} from "@microsoft/signalr";
import config from "../types/config";

export function createSignalRConnection(): HubConnection {
  const url = config.notificationsUrl;

  const connection = new HubConnectionBuilder()
    .withUrl(url, {
      skipNegotiation: true,
      transport: HttpTransportType.WebSockets,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(
      import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning
    )
    .build();

  return connection;
}

export async function startConnection(
  connection: HubConnection
): Promise<void> {
  try {
    await connection.start();
    console.log("[SignalR] Connected successfully");
  } catch (err) {
    console.error("[SignalR] Connection failed:", err);

    setTimeout(() => startConnection(connection), 5000);
  }
}
