namespace DataProcessor.Application.Models;

public class EnergyModel
{
    public string Id { get; set; } = null!;
    public string RoomId { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public double Amount { get; set; }
}
