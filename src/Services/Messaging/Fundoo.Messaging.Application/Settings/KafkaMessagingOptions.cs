namespace Fundoo.Messaging.Application.Settings;

public sealed class KafkaMessagingOptions
{
    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string EmailTopic { get; set; } = "fundoo.email.v1";
    public string ConsumerGroup { get; set; } = "fundoo-messaging-email";
    public string ClientId { get; set; } = "fundoo-messaging";
}
