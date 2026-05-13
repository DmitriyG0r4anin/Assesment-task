namespace NotificationsService.Models;

public record MotionDetectedMessage
{
    public string RoomName { get; init; } = string.Empty;
    public bool IsDetected { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
