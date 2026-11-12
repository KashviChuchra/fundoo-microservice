using Fundoo.Messaging.Application.DTOs;
using Fundoo.Messaging.Application.Interfaces;
using Fundoo.Messaging.Application.Settings;
using Fundoo.Messaging.Domain.Entities;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Fundoo.Messaging.Application.Services;

/// <summary>
/// Sends emails via SMTP using MailKit
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SendAsync(SendEmailRequest request,CancellationToken cancellationToken = default)
    {
        // Map DTO → domain entity for any downstream domain logic / auditing
        var message = new EmailMessage
        {
            RecipientName = request.RecipientName,
            To            = request.To,
            Subject       = request.Subject,
            Body          = request.Body,
            HtmlBody      = request.HtmlBody
        };

        var mimeMessage = BuildMimeMessage(message);

        try
        {
            using var client = new SmtpClient();

            var secureSocket = _settings.EnableSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(
                _settings.SmtpHost,
                _settings.SmtpPort,
                secureSocket,
                cancellationToken);

            await client.AuthenticateAsync(
                _settings.SmtpUser,
                _settings.SmtpPassword,
                cancellationToken);

            await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            _logger.LogInformation(
                "Email sent successfully to {Recipient} with subject '{Subject}'.",
                request.To, request.Subject);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {Recipient} with subject '{Subject}'.",
                request.To, request.Subject);

            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private MimeMessage BuildMimeMessage(EmailMessage message)
    {
        var mime = new MimeMessage();

        mime.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));

        mime.To.Add(new MailboxAddress(message.RecipientName, message.To));
        mime.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder
        {
            TextBody = message.Body
        };

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            bodyBuilder.HtmlBody = message.HtmlBody;
        }

        mime.Body = bodyBuilder.ToMessageBody();
        return mime;
    }
}
