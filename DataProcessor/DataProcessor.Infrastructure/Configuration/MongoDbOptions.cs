namespace DataProcessor.Infrastructure.Configuration;

public class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "data_processor";

    public required MongoDbCollectionsOptions Collections { get; set; }
}

public class MongoDbCollectionsOptions
{
    public string Rooms { get; set; } = "rooms";
    public string Motion { get; set; } = "motions";
    public string AirQualities { get; set; } = "air_qualities";
    public string Energies { get; set; } = "energies";
}
