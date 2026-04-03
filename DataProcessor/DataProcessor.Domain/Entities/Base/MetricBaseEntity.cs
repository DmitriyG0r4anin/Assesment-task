using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataProcessor.Domain.Entities.Base;

public abstract class MetricBaseEntity : BaseEntity
{
    [BsonElement("roomId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string RoomId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
