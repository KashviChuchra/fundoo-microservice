using System.ComponentModel.DataAnnotations;

namespace Fundoo.User.Api.Contracts
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;
    }
}
