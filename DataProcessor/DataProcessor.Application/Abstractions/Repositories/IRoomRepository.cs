using DataProcessor.Domain.Entities;

namespace DataProcessor.Application.Abstractions.Repositories;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Room?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Room> GetOrCreateAsync(string name, CancellationToken ct = default);
    Task<List<Room>> GetAllAsync(CancellationToken ct = default);
}
