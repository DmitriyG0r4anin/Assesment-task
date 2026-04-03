using DataProcessor.Domain.Entities.Base;

namespace DataProcessor.Application.Abstractions.Repositories.Base;

public interface IMetricBaseRepository<T> : IBaseRepository<T>
    where T : MetricBaseEntity
{
    Task<IReadOnlyList<T>> GetAllAsync(
        string? roomId = null,
        DateTime? timestampStart = null,
        DateTime? timestampEnd = null,
        CancellationToken cancellationToken = default);
}
