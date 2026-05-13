using System.Text.Json.Serialization;
using NotificationsService.Models;

namespace NotificationsService.Serialization;

/// <summary>
/// Source-generated JSON for SignalR hub payloads (reflection disabled under PublishAot).
/// CamelCase matches typical JavaScript clients and default JsonHubProtocol expectations.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(MotionEvent))]
[JsonSerializable(typeof(string))]
internal partial class SignalRPayloadJsonContext : JsonSerializerContext;
