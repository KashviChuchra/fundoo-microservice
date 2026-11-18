using System.ComponentModel.DataAnnotations;

namespace Fundoo.User.Api.Contracts;

public sealed class GoogleLoginRequest
{
    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string RedirectUri { get; set; } = string.Empty;
}
