namespace NotificationsService.Configuration;

public class KafkaConfig
{
    public const string SectionName = "Kafka";
    public string Brokers { get; set; } = "broker:29092";
    public string MotionTopic { get; set; } = "motion-events";
    public string GroupId { get; set; } = "notifications-group";
}
