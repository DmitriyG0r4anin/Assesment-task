using DataProcessor.Domain.Entities;
using DataProcessor.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DataProcessor.Infrastructure.Persistence;

public class MongoDbContext(IOptions<MongoDbOptions> settings)
{
    private readonly MongoDbOptions _configuration = settings.Value;

    private readonly IMongoDatabase _database = new MongoClient(settings.Value.ConnectionString).GetDatabase(settings.Value.DatabaseName);

    public IMongoCollection<Room> Rooms => _database.GetCollection<Room>(_configuration.Collections.Rooms);
    public IMongoCollection<AirQuality> AirQualities => _database.GetCollection<AirQuality>(_configuration.Collections.AirQualities);
    public IMongoCollection<Energy> Energies => _database.GetCollection<Energy>(_configuration.Collections.Energies);
    public IMongoCollection<Motion> Motions => _database.GetCollection<Motion>(_configuration.Collections.Motion);
}
