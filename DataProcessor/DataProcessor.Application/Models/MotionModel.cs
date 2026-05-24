namespace DataProcessor.Application.Models;

public class MotionModel : MetricBaseModel
{
    public DateTime Timestamp { get; set; }
    public bool IsDetected { get; set; }
}
