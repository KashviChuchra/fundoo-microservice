namespace Fundoo.User.Infrastructure.Authentication;

public sealed class GoogleOAuthOptions
{
    public const string SectionName = "GoogleOAuth";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string[] AllowedRedirectUris { get; set; } = [];
}