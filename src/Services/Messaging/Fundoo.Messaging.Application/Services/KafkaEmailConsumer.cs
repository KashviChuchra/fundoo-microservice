using System.Text.Json;
using Confluent.Kafka;
using Fundoo.Messaging.Application.DTOs;
using Fundoo.Messaging.Application.Interfaces;
using Fundoo.Messaging.Application.Settings;
using Fundoo.Messaging.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fundoo.Messaging.Application.Services;

public sealed class KafkaEmailConsumer : BackgroundService
{
    private readonly KafkaMessagingOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaEmailConsumer> _logger;

    public KafkaEmailConsumer(
        IOptions<KafkaMessagingOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaEmailConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Kafka email consumer is disabled.");
            return;
        }

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroup,
            ClientId = _options.ClientId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnablePartitionEof = false
        }).Build();

        consumer.Subscribe(_options.EmailTopic);
        _logger.LogInformation("Kafka email consumer subscribed to {Topic} with group {GroupId}.", _options.EmailTopic, _options.ConsumerGroup);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string> result;
                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka email event consumption failed.");
                    continue;
                }

                EmailNotificationRequested? message;
                try
                {
                    message = JsonSerializer.Deserialize<EmailNotificationRequested>(result.Message.Value);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Discarding malformed Kafka email event at {TopicPartitionOffset}.", result.TopicPartitionOffset);
                    consumer.Commit(result);
                    continue;
                }

                if (message is null || string.IsNullOrWhiteSpace(message.To) || string.IsNullOrWhiteSpace(message.Subject))
                {
                    _logger.LogError("Discarding invalid Kafka email event at {TopicPartitionOffset}.", result.TopicPartitionOffset);
                    consumer.Commit(result);
                    continue;
                }

                using IServiceScope scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                bool sent = emailService.SendAsync(new SendEmailRequest
                {
                    RecipientName = message.RecipientName,
                    To = message.To,
                    Subject = message.Subject,
                    Body = message.Body,
                    HtmlBody = message.HtmlBody
                }, stoppingToken).GetAwaiter().GetResult();

                if (sent)
                {
                    consumer.Commit(result);
                    _logger.LogInformation("Processed Kafka email event {EventId}.", message.EventId);
                }
                else
                {
                    _logger.LogWarning("Email delivery failed for Kafka event {EventId}; the message will remain uncommitted.", message.EventId);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            consumer.Close();
        }
    }
}
