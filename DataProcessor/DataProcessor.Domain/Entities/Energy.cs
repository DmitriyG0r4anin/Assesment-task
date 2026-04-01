using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataProcessor.Domain.Entities;

public class Energy
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("roomId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RoomId { get; set; } = null!;

    [BsonElement("amount")]
    public float Amount { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }
}
