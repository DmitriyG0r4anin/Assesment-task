using DataProcessor.Application.Abstractions.Repositories;
using DataProcessor.Domain.Entities;
using MongoDB.Driver;

namespace DataProcessor.Infrastructure.Persistence.Repositories;

public class MotionRepository(MongoDbContext context) : IMotionRepository
{
    public async Task<Motion?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await context.Motions
            .Find(m => m.Id == id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<Motion>> GetAllAsync(
        string? roomId = null,
        DateTime? timestamp = null,
        CancellationToken ct = default)
    {
        var builder = Builders<Motion>.Filter;
        var filter = builder.Empty;

        if (!string.IsNullOrEmpty(roomId))
            filter &= builder.Eq(m => m.RoomId, roomId);

        if (timestamp.HasValue)
            filter &= builder.Eq(m => m.Timestamp, timestamp.Value);

        return await context.Motions
            .Find(filter)
            .ToListAsync(ct);
    }

    public async Task InsertAsync(Motion entity, CancellationToken ct = default)
    {
        await context.Motions.InsertOneAsync(entity, cancellationToken: ct);
    }
}
