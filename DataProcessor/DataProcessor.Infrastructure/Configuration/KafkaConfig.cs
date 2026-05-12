namespace DataProcessor.Infrastructure.Configuration;

public class KafkaConfig
{
    public const string SectionName = "Kafka";
    public string Brokers { get; set; } = "broker:29092";
    public string MetricTopic { get; set; } = "meter-data";
    public string GroupId { get; set; } = "data-processor-group";
    public string MotionTopic { get; set; } = "motion-events";
}
