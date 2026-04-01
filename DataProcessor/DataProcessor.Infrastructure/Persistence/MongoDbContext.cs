using DataProcessor.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DataProcessor.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<Room> Rooms => _database.GetCollection<Room>("rooms");
    public IMongoCollection<AirQuality> AirQualities => _database.GetCollection<AirQuality>("air_qualities");
    public IMongoCollection<Energy> Energies => _database.GetCollection<Energy>("energies");
    public IMongoCollection<Motion> Motions => _database.GetCollection<Motion>("motions");
}
