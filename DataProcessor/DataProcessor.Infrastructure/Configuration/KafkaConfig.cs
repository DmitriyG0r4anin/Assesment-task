namespace DataProcessor.Infrastructure.Configuration;

public class KafkaConfig
{
    public const string SectionName = "Kafka";
    public string Brokers { get; set; } = "broker:29092";
    public string Topic { get; set; } = "meter-data";
    public string GroupId { get; set; } = "data-processor-group";
}
