namespace DataProcessor.Application.Models;

public class AirQualityModel
{
    public string Id { get; set; } = null!;
    public string RoomId { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public int Pm25 { get; set; }
    public int Co2 { get; set; }
    public int Humidity { get; set; }
}
