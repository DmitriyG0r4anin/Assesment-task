using System.Text.Json.Serialization;
using NotificationsService.Models;

namespace NotificationsService.Serialization;

/// <summary>
/// Source-generated JSON metadata for Kafka payloads (required when reflection-based serialization is disabled, e.g. with PublishAot).
/// </summary>
[JsonSerializable(typeof(MotionDetectedMessage))]
internal partial class MotionMessageJsonContext : JsonSerializerContext;
