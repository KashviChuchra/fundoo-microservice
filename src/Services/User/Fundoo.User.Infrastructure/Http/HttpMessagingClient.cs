using Fundoo.User.Application.Interface;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Fundoo.User.Infrastructure.Http
{
    
    public sealed class HttpMessagingClient : IMessagingClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpMessagingClient> _logger;

        public HttpMessagingClient(HttpClient httpClient, ILogger<HttpMessagingClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SendLoginOtpEmailAsync(
            string recipientName,
            string toEmail,
            string code,
            CancellationToken cancellationToken)
        {
            var payload = new
            {
                recipientName,
                to = toEmail,
                subject = "Fundoo — Your login verification code",
                body = $"Hi {recipientName},\n\nYour Fundoo login verification code is {code}. It expires in 10 minutes.\n\nIf you did not try to sign in, reset your password and contact support.",
                htmlBody = $"<p>Hi <strong>{recipientName}</strong>,</p><p>Your Fundoo login verification code is:</p><p style=\"font-size:1.5em;letter-spacing:4px;\"><strong>{code}</strong></p><p>This code expires in <strong>10 minutes</strong>.</p>"
            };

            var response = await _httpClient.PostAsJsonAsync("api/email/send", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Messaging service failed with status code {(int)response.StatusCode}.");
            }
        }

        public async Task SendPasswordResetEmailAsync(
            string recipientName,
            string toEmail,
            string resetToken,
            CancellationToken cancellationToken)
        {
            string plainBody =
                $"Hi {recipientName},\n\n" +
                $"We received a request to reset your Fundoo password.\n\n" +
                $"Your password-reset token is:\n\n{resetToken}\n\n" +
                $"This token is valid for 1 hour. If you did not request a password reset, you can safely ignore this email.\n\n" +
                $"— The Fundoo Team";

            string htmlBody =
                $"<p>Hi <strong>{recipientName}</strong>,</p>" +
                $"<p>We received a request to reset your <strong>Fundoo</strong> password.</p>" +
                $"<p>Your password-reset token is:</p>" +
                $"<p style=\"font-size:1.2em;letter-spacing:2px;\"><strong>{resetToken}</strong></p>" +
                $"<p>This token is valid for <strong>1 hour</strong>. " +
                $"If you did not request a password reset, you can safely ignore this email.</p>" +
                $"<p>— The Fundoo Team</p>";

            var payload = new
            {
                recipientName,
                to = toEmail,
                subject = "Fundoo — Password Reset Request",
                body = plainBody,
                htmlBody
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    "api/email/send",
                    payload,
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Messaging service returned {StatusCode} for reset email.",(int)response.StatusCode);

                    throw new HttpRequestException($"Messaging service failed with status code {(int)response.StatusCode}.");
                }

                _logger.LogInformation("Password-reset email dispatched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Failed to send password-reset email.");
                throw;
            }
        }

    }
}
