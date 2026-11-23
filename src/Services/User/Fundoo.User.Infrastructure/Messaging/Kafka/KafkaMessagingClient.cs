using System.Text.Json;
using Confluent.Kafka; // provides kafka producer+ messaging types
using Fundoo.Messaging.Domain.Events;
using Fundoo.User.Application.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fundoo.User.Infrastructure.Messaging.Kafka;

public sealed class KafkaMessagingClient : IKafkaMessagingClient, IDisposable
{
    private readonly IProducer<string, string> _producer;     // send messages to broker - key,value
    private readonly KafkaMessagingOptions _options;         // message broker, topic, client settings
    private readonly ILogger<KafkaMessagingClient> _logger;

    public KafkaMessagingClient(IOptions<KafkaMessagingOptions> options, ILogger<KafkaMessagingClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,    // tells where to find kafka
            ClientId = _options.ClientId,                    // helps prevent duplicate Kafka writes caused by producer retries.
            Acks = Acks.All,                                // asks Kafka to confirm the write was acknowledged by its in-sync replicas.
            EnableIdempotence = true
        }).Build();      // create producer instance
    }

    public Task SendPasswordResetEmailAsync(string recipientName, string toEmail, string resetToken, CancellationToken cancellationToken)
    {
        string body = $"Hi {recipientName},\n\n" +
            "We received a request to reset your Fundoo password.\n\n" +
            $"Your password-reset token is:\n\n{resetToken}\n\n" +
            "This token is valid for 1 hour. If you did not request a password reset, you can safely ignore this email.\n\n" +
            "— The Fundoo Team";
        string htmlBody = $"<p>Hi <strong>{recipientName}</strong>,</p>" +
            "<p>We received a request to reset your <strong>Fundoo</strong> password.</p>" +
            $"<p>Your password-reset token is:</p><p style=\"font-size:1.2em;letter-spacing:2px;\"><strong>{resetToken}</strong></p>" +
            "<p>This token is valid for <strong>1 hour</strong>.</p>";

        return PublishAsync(new EmailNotificationRequested(Guid.NewGuid(), recipientName, toEmail, "Fundoo — Password Reset Request", body, htmlBody), cancellationToken);
    }
    public Task SendLoginOtpEmailAsync(string recipientName,string toEmail,string code,CancellationToken cancellationToken)
    {
        string body = $"Hi {recipientName},\n\nYour Fundoo login verification code is {code}. It expires in 10 minutes.";
        string htmlBody = $"<p>Hi <strong>{recipientName}</strong>,</p><p>Your Fundoo login verification code is:</p>" +
            $"<p style=\"font-size:1.5em;letter-spacing:4px;\"><strong>{code}</strong></p><p>This code expires in <strong>10 minutes</strong>.</p>";

        return PublishAsync(new EmailNotificationRequested(Guid.NewGuid(), recipientName, toEmail, "Fundoo — Your login verification code", body, htmlBody), cancellationToken);
    }
    private async Task PublishAsync(EmailNotificationRequested message, CancellationToken cancellationToken)
    {
        string payload = JsonSerializer.Serialize(message);
        DeliveryResult<string, string> result = await _producer.ProduceAsync(
            _options.EmailTopic,
            new Message<string, string> { Key = message.To, Value = payload },
            cancellationToken);

        if (result.Status is not PersistenceStatus.Persisted and not PersistenceStatus.PossiblyPersisted)
        {
            throw new InvalidOperationException("Kafka did not acknowledge the email event.");
        }

        _logger.LogInformation("Published email event {EventId} to Kafka topic {Topic}.", message.EventId, _options.EmailTopic);
    }

    public void Dispose() => _producer.Dispose();
}
