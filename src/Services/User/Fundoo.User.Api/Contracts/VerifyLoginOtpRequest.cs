using System.ComponentModel.DataAnnotations;

namespace Fundoo.User.Api.Contracts;

public class VerifyLoginOtpRequest
{
    [Required]
    public Guid ChallengeId { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}
