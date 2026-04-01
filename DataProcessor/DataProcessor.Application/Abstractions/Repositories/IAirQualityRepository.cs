using DataProcessor.Domain.Entities;

namespace DataProcessor.Application.Abstractions.Repositories;

public interface IAirQualityRepository
{
    Task<AirQuality?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<AirQuality>> GetAllAsync(string? roomId = null, DateTime? timestamp = null, CancellationToken ct = default);
    Task InsertAsync(AirQuality entity, CancellationToken ct = default);
}
