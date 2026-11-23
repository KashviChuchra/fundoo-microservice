namespace Fundoo.Messaging.Domain.Events;

public sealed record EmailNotificationRequested(Guid EventId, string RecipientName, string To, string Subject, string Body,string? HtmlBody);
