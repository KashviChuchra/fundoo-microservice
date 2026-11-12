using System.ComponentModel.DataAnnotations;

namespace Fundoo.User.Api.Contracts;

public sealed class GoogleLoginRequest
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
}
