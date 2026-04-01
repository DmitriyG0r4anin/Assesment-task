using System.Text.Json.Serialization;

namespace DataProcessor.Infrastructure.Messaging.Models;

public class KafkaMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("payload")]
    public System.Text.Json.JsonElement Payload { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public class AirQualityPayload
{
    [JsonPropertyName("co2")]
    public int Co2 { get; set; }

    [JsonPropertyName("pm25")]
    public int Pm25 { get; set; }

    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }
}

public class MotionPayload
{
    [JsonPropertyName("motionDetected")]
    public bool MotionDetected { get; set; }
}

public class EnergyPayload
{
    [JsonPropertyName("energy")]
    public float Energy { get; set; }
}
