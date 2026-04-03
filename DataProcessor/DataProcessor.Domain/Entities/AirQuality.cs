using DataProcessor.Domain.Entities.Base;

namespace DataProcessor.Domain.Entities;

public class AirQuality : MetricBaseEntity
{
    public int Pm25 { get; set; }

    public int Co2 { get; set; }

    public int Humidity { get; set; }
}
