namespace Fundoo.User.Infrastructure.Messaging.Kafka;

public sealed class KafkaMessagingOptions
{
    public bool UseKafka { get; set; }
    public bool FallbackToHttp { get; set; } = true;
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string EmailTopic { get; set; } = "fundoo.email.v1";
    public string ClientId { get; set; } = "fundoo-user";
}
