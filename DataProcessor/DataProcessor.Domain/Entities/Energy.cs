using DataProcessor.Domain.Entities.Base;

namespace DataProcessor.Domain.Entities;

public class Energy : MetricBaseEntity
{
    public double Amount { get; set; }
}
