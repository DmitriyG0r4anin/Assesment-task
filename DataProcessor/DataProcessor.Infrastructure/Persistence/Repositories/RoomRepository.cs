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

    public async Task<Room> CreateByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var room = new Room { Name = name };
        await Collection.InsertOneAsync(room, cancellationToken: cancellationToken);
        return room;
    }
}
