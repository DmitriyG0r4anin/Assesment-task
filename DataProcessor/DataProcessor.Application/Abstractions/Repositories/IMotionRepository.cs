using DataProcessor.Domain.Entities;

namespace DataProcessor.Application.Abstractions.Repositories;

public interface IMotionRepository
{
    Task<Motion?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<Motion>> GetAllAsync(string? roomId = null, DateTime? timestamp = null, CancellationToken ct = default);
    Task InsertAsync(Motion entity, CancellationToken ct = default);
}
