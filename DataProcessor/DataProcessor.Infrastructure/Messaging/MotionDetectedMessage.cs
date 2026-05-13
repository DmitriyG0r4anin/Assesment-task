namespace DataProcessor.Infrastructure.Messaging;

public class MotionDetectedMessage
{
    public string RoomName { get; set; } = string.Empty;
    public bool IsDetected { get; set; }
    public DateTime Timestamp { get; set; }
}
