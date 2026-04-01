using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataProcessor.Domain.Entities;

public class Room
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;
}
