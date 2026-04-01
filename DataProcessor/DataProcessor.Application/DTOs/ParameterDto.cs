namespace DataProcessor.Application.DTOs;

public class ParameterDto
{
    public string Id { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string RoomName { get; set; } = null!;
    public DateTime Timestamp { get; set; }

    // Air Quality fields (nullable)
    public int? Pm25 { get; set; }
    public int? Co2 { get; set; }
    public int? Humidity { get; set; }

    // Energy fields (nullable)
    public float? Amount { get; set; }

    // Motion fields (nullable)
    public bool? IsDetected { get; set; }
}
