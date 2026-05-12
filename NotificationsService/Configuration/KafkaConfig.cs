namespace NotificationsService.Configuration;

public class KafkaConfig
{
    public const string SectionName = "Kafka";
    public string Brokers { get; set; }
    public string MotionTopic { get; set; } = "motion-events";
    public string GroupId { get; set; } = "notifications-group";
}
