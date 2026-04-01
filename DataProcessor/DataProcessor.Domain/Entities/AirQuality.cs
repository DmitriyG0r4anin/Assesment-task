using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataProcessor.Domain.Entities;

public class AirQuality
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("roomId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RoomId { get; set; } = null!;

    [BsonElement("pm25")]
    public int Pm25 { get; set; }

    [BsonElement("co2")]
    public int Co2 { get; set; }

    [BsonElement("humidity")]
    public int Humidity { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }
}
