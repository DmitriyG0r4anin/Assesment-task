using DataProcessor.Domain.Entities;

namespace DataProcessor.Application.Abstractions.Repositories;

public interface IEnergyRepository
{
    Task<Energy?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<Energy>> GetAllAsync(string? roomId = null, DateTime? timestamp = null, CancellationToken ct = default);
    Task InsertAsync(Energy entity, CancellationToken ct = default);
}
