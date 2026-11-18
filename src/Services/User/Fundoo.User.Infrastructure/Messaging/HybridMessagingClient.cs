using Fundoo.User.Application.Interface;
using Fundoo.User.Infrastructure.Messaging.Http;
using Fundoo.User.Infrastructure.Messaging.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fundoo.User.Infrastructure.Messaging;

public sealed class HybridMessagingClient : IMessagingClient
{
    private readonly IKafkaMessagingClient _kafkaClient;
    private readonly HttpMessagingClient _httpClient;
    private readonly KafkaMessagingOptions _options;
    private readonly ILogger<HybridMessagingClient> _logger;

    public HybridMessagingClient(
        IKafkaMessagingClient kafkaClient,
        HttpMessagingClient httpClient,
        IOptions<KafkaMessagingOptions> options,
        ILogger<HybridMessagingClient> logger)
    {
        _kafkaClient = kafkaClient;
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string recipientName, string toEmail, string resetToken, CancellationToken cancellationToken)
    {
        return SendAsync(
            () => _kafkaClient.SendPasswordResetEmailAsync(recipientName, toEmail, resetToken, cancellationToken),
            () => _httpClient.SendPasswordResetEmailAsync(recipientName, toEmail, resetToken, cancellationToken),
            cancellationToken);
    }

    public Task SendLoginOtpEmailAsync(string recipientName, string toEmail, string code, CancellationToken cancellationToken)
    {
        return SendAsync(
            () => _kafkaClient.SendLoginOtpEmailAsync(recipientName, toEmail, code, cancellationToken),
            () => _httpClient.SendLoginOtpEmailAsync(recipientName, toEmail, code, cancellationToken),
            cancellationToken);
    }

    private async Task SendAsync(Func<Task> kafkaSend, Func<Task> httpSend, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_options.UseKafka)
        {
            await httpSend();
            return;
        }

        try
        {
            await kafkaSend();
        }
        catch (Exception ex) when (_options.FallbackToHttp && ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Kafka email publishing failed; using the configured HTTP messaging fallback.");
            await httpSend();
        }
    }
}
