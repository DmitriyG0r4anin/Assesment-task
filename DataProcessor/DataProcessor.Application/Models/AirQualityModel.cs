namespace DataProcessor.Application.Models;

public class AirQualityModel : MetricBaseModel
{
    public DateTime Timestamp { get; set; }
    public int Pm25 { get; set; }
    public int Co2 { get; set; }
    public int Humidity { get; set; }
}
