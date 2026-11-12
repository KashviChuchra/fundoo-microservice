using Fundoo.User.Application.Interface;
using Fundoo.User.Application.Models;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace Fundoo.User.Infrastructure.Security;

public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly IConfiguration _configuration;

    public GoogleTokenValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idToken) || cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        string clientId = _configuration["Google:ClientId"]
            ?? throw new InvalidOperationException("Google:ClientId is not configured.");

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                });

            if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
            {
                return null;
            }

            return new GoogleUserInfo(
                payload.Subject,
                payload.Email,
                payload.GivenName ?? payload.Name ?? string.Empty,
                payload.FamilyName ?? string.Empty);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
