namespace DataProcessor.Application.Abstractions.Repositories;

public interface IRoomRepository : IBaseRepository<Room>
{
    Task<Room?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Room> CreateByNameAsync(string name, CancellationToken cancellationToken = default);
}
