namespace Fundoo.Messaging.Application.Settings
{
    /// <summary>
    /// SMTP configuration values loaded from appsettings.json
    /// under the "EmailSettings" section.
    /// </summary>
    public sealed class EmailSettings
    {
        public string SmtpHost { get; init; } = string.Empty;
        public int SmtpPort { get; init; } = 587;
        public string SmtpUser { get; init; } = string.Empty;
        public string SmtpPassword { get; init; } = string.Empty;
        public string SenderName { get; init; } = "Fundoo";
        public string SenderEmail { get; init; } = string.Empty;
        public bool EnableSsl { get; init; } = true;
    }
}
