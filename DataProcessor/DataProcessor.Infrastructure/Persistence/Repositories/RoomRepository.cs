using DataProcessor.Domain.Entities;
using MongoDB.Driver;

namespace DataProcessor.Infrastructure.Persistence.Repositories;

public class RoomRepository(IMongoDatabase database) : BaseRepository<Room>(database), IRoomRepository
{
    public async Task<Room?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await Collection
            .Find(r => r.Name == name)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Room> GetOrCreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var existing = await GetByNameAsync(name, cancellationToken);
        if (existing is not null)
            return existing;

        var room = new Room { Name = name };
        await Collection.InsertOneAsync(room, cancellationToken: cancellationToken);
        return room;
    }
}
