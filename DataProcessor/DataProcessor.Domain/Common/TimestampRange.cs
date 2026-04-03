namespace DataProcessor.Domain.Common;

public class TimestampRange
{
    public DateTime TimestampStart { get; set; }
    public DateTime TimestampEnd { get; set; } = DateTime.UtcNow;
}
