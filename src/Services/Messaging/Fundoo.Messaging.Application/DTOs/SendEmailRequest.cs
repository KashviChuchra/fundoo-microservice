using System.ComponentModel.DataAnnotations;

namespace Fundoo.Messaging.Application.DTOs
{
    /// <summary>
    /// DTO used by the API layer to receive an email send request.
    /// </summary>
    public sealed class SendEmailRequest
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Recipient name is required.")]
        [MaxLength(200, ErrorMessage = "Recipient name cannot exceed 200 characters.")]
        public string RecipientName { get; init; } = string.Empty;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Recipient email is required.")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
        [MaxLength(256, ErrorMessage = "Email address cannot exceed 256 characters.")]
        public string To { get; init; } = string.Empty;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Subject is required.")]
        [MaxLength(998, ErrorMessage = "Subject cannot exceed 998 characters.")]
        public string Subject { get; init; } = string.Empty;

        [Required(AllowEmptyStrings = false, ErrorMessage = "Body is required.")]
        public string Body { get; init; } = string.Empty;
        public string? HtmlBody { get; init; }
    }
}
