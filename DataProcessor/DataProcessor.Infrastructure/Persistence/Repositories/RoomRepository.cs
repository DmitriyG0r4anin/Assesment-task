using DataProcessor.Application.Abstractions.Repositories;
using DataProcessor.Domain.Entities;
using MongoDB.Driver;

namespace DataProcessor.Infrastructure.Persistence.Repositories;

public class RoomRepository(MongoDbContext context) : IRoomRepository
{
    public async Task<Room?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await context.Rooms
            .Find(r => r.Id == id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Room?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await context.Rooms
            .Find(r => r.Name == name)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Room> GetOrCreateAsync(string name, CancellationToken ct = default)
    {
        var existing = await GetByNameAsync(name, ct);
        if (existing is not null)
            return existing;

        var room = new Room { Name = name };
        await context.Rooms.InsertOneAsync(room, cancellationToken: ct);
        return room;
    }

    public async Task<List<Room>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Rooms
            .Find(_ => true)
            .ToListAsync(ct);
    }
}
