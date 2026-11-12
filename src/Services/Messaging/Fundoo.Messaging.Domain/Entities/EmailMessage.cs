namespace Fundoo.Messaging.Domain.Entities
{
    
    public class EmailMessage
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string RecipientName { get; init; } = string.Empty;
        public string To { get; init; } = string.Empty;
        public string Subject { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
        public string? HtmlBody { get; init; }
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
        public bool IsSent { get; set; }
        public DateTime? SentAtUtc { get; set; }
    }
}
