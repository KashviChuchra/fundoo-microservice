namespace Fundoo.User.Application.Interface
{

    public interface IMessagingClient
    {
        /// <summary>
        /// Sends a password-reset email via the Messaging microservice.
        /// </summary>
        Task SendPasswordResetEmailAsync(string recipientName,string toEmail,string resetToken,CancellationToken cancellationToken);
        Task SendLoginOtpEmailAsync(string recipientName, string toEmail, string code, CancellationToken cancellationToken);
    }
}
