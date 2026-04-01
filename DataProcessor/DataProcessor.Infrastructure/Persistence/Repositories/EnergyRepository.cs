using DataProcessor.Application.Abstractions.Repositories;
using DataProcessor.Domain.Entities;
using MongoDB.Driver;

namespace DataProcessor.Infrastructure.Persistence.Repositories;

public class EnergyRepository(MongoDbContext context) : IEnergyRepository
{
    public async Task<Energy?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await context.Energies
            .Find(e => e.Id == id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<Energy>> GetAllAsync(
        string? roomId = null,
        DateTime? timestamp = null,
        CancellationToken ct = default)
    {
        var builder = Builders<Energy>.Filter;
        var filter = builder.Empty;

        if (!string.IsNullOrEmpty(roomId))
            filter &= builder.Eq(e => e.RoomId, roomId);

        if (timestamp.HasValue)
            filter &= builder.Eq(e => e.Timestamp, timestamp.Value);

        return await context.Energies
            .Find(filter)
            .ToListAsync(ct);
    }

    public async Task InsertAsync(Energy entity, CancellationToken ct = default)
    {
        await context.Energies.InsertOneAsync(entity, cancellationToken: ct);
    }
}
