using Fundoo.Messaging.Application.DTOs;

namespace Fundoo.Messaging.Application.Interfaces
{
    /// <summary>
    /// Contract for the email-sending service.
    /// </summary>
    public interface IEmailService
    {
        Task<bool> SendAsync(SendEmailRequest request,CancellationToken cancellationToken = default);
    }
}
