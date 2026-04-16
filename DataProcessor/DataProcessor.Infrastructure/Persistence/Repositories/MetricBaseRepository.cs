using DataProcessor.Domain.Entities.Base;
using MongoDB.Driver;

namespace DataProcessor.Infrastructure.Persistence.Repositories;

public class MetricBaseRepository<T>(IMongoDatabase database)
    : BaseRepository<T>(database), IMetricBaseRepository<T>
    where T : MetricBaseEntity
{
    public async Task<IReadOnlyList<T>> GetAllAsync(
        string? roomId,
        DateTime? timestampStart,
        DateTime? timestampEnd,
        CancellationToken cancellationToken = default)
    {
        var builder = Builders<T>.Filter;
        var filter = builder.Empty;

        if (!string.IsNullOrEmpty(roomId))
            filter &= builder.Eq(e => e.RoomId, roomId);

        if (timestampStart.HasValue)
            filter &= builder.Gte(e => e.Timestamp, timestampStart.Value);

        if (timestampEnd.HasValue)
            filter &= builder.Lte(e => e.Timestamp, timestampEnd.Value);

        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }
}
