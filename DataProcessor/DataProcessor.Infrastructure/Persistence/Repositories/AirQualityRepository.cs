using DataProcessor.Application.Abstractions.Repositories;
using DataProcessor.Domain.Entities;
using MongoDB.Driver;

namespace DataProcessor.Infrastructure.Persistence.Repositories;

public class AirQualityRepository(MongoDbContext context) : IAirQualityRepository
{
    public async Task<AirQuality?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await context.AirQualities
            .Find(a => a.Id == id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<AirQuality>> GetAllAsync(
        string? roomId = null,
        DateTime? timestamp = null,
        CancellationToken ct = default)
    {
        var builder = Builders<AirQuality>.Filter;
        var filter = builder.Empty;

        if (!string.IsNullOrEmpty(roomId))
            filter &= builder.Eq(a => a.RoomId, roomId);

        if (timestamp.HasValue)
            filter &= builder.Eq(a => a.Timestamp, timestamp.Value);

        return await context.AirQualities
            .Find(filter)
            .ToListAsync(ct);
    }

    public async Task InsertAsync(AirQuality entity, CancellationToken ct = default)
    {
        await context.AirQualities.InsertOneAsync(entity, cancellationToken: ct);
    }
}
