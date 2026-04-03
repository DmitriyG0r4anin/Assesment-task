using DataProcessor.Domain.Entities.Base;

namespace DataProcessor.Domain.Entities;

public class Motion : MetricBaseEntity
{
    public bool IsDetected { get; set; } = true;
}
