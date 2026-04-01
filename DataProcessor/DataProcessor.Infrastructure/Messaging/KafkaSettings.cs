namespace DataProcessor.Infrastructure.Messaging;

public class KafkaSettings
{
    public string Brokers { get; set; } = "broker:29092";
    public string Topic { get; set; } = "meter-data";
    public string GroupId { get; set; } = "data-processor-group";
}
