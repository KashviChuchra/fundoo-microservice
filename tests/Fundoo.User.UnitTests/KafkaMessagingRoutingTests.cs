using System.Net;
using System.Net.Http;
using Fundoo.User.Infrastructure.Messaging;
using Fundoo.User.Infrastructure.Messaging.Http;
using Fundoo.User.Infrastructure.Messaging.Kafka;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Fundoo.User.UnitTests;

public sealed class KafkaMessagingRoutingTests
{
    [Fact]
    public async Task UseKafkaFalse_SelectsRetainedHttpClient()
    {
        var kafka = new Mock<IKafkaMessagingClient>();
        var handler = new RecordingHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://messaging.local/") };
        var fallback = new HttpMessagingClient(httpClient, NullLogger<HttpMessagingClient>.Instance);
        var client = new HybridMessagingClient(
            kafka.Object,
            fallback,
            Options.Create(new KafkaMessagingOptions { UseKafka = false }),
            NullLogger<HybridMessagingClient>.Instance);

        await client.SendLoginOtpEmailAsync("Jane Doe", "jane@example.com", "123456", CancellationToken.None);

        Assert.Equal(1, handler.RequestCount);
        kafka.Verify(k => k.SendLoginOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UseKafkaTrue_SelectsKafkaWithoutCallingHttp()
    {
        var kafka = new Mock<IKafkaMessagingClient>();
        kafka.Setup(k => k.SendLoginOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new RecordingHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://messaging.local/") };
        var fallback = new HttpMessagingClient(httpClient, NullLogger<HttpMessagingClient>.Instance);
        var client = new HybridMessagingClient(
            kafka.Object,
            fallback,
            Options.Create(new KafkaMessagingOptions { UseKafka = true, FallbackToHttp = false }),
            NullLogger<HybridMessagingClient>.Instance);

        await client.SendLoginOtpEmailAsync("Jane Doe", "jane@example.com", "123456", CancellationToken.None);

        Assert.Equal(0, handler.RequestCount);
        kafka.Verify(k => k.SendLoginOtpEmailAsync("Jane Doe", "jane@example.com", "123456", It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
