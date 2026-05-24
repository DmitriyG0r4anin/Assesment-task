namespace DataProcessor.Application.Models;

public abstract class MetricBaseModel
{
    public required string Id { get; set; }
    public required string RoomId { get; set; }
}
