namespace DataProcessor.Application.Models;

public class MotionModel
{
    public string Id { get; set; } = null!;
    public string RoomId { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public bool IsDetected { get; set; }
}
