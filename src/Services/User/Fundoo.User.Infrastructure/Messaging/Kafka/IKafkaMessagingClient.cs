namespace Fundoo.User.Infrastructure.Messaging.Kafka;

public interface IKafkaMessagingClient
{
    Task SendPasswordResetEmailAsync(string recipientName, string toEmail, string resetToken, CancellationToken cancellationToken);
    Task SendLoginOtpEmailAsync(string recipientName, string toEmail, string code, CancellationToken cancellationToken);
}
