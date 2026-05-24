namespace DataProcessor.Application.Models;

public class EnergyModel : MetricBaseModel
{
    public DateTime Timestamp { get; set; }
    public double Amount { get; set; }
}
