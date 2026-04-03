using DataProcessor.Application.Abstractions.Repositories;
using DataProcessor.Domain.Entities;
using MongoDB.Driver;

namespace DataProcessor.Infrastructure.Persistence.Repositories;

public class RoomRepository(MongoDbContext context) : BaseRepository<Room>(context.Rooms.Database, context.Rooms.CollectionNamespace.CollectionName), IRoomRepository
{
    private readonly IMongoCollection<Room> _collection = context.Rooms;

    public async Task<Room?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(r => r.Name == name)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Room> GetOrCreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var existing = await GetByNameAsync(name, cancellationToken);
        if (existing is not null)
            return existing;

        var room = new Room { Name = name };
        await _collection.InsertOneAsync(room, cancellationToken: cancellationToken);
        return room;
    }
}
