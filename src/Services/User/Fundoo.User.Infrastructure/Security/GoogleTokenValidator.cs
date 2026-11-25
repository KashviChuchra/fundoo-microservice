using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Fundoo.User.Application.Interface;
using Fundoo.User.Application.Models;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace Fundoo.User.Infrastructure.Authentication;

public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly HttpClient _httpClient;
    private readonly GoogleOAuthOptions _options;

    public GoogleTokenValidator(
        HttpClient httpClient,
        IOptions<GoogleOAuthOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<GoogleUserInfo?> ExchangeCodeAndValidateAsync(
        string authorizationCode,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authorizationCode) ||
            string.IsNullOrWhiteSpace(redirectUri))
        {
            return null;
        }

        // The redirect URI must be one of the URIs configured
        // for this application. Do not accept arbitrary URLs.
        bool redirectAllowed = _options.AllowedRedirectUris
            .Any(uri => string.Equals(
                uri,
                redirectUri,
                StringComparison.Ordinal));

        if (!redirectAllowed)
        {
            return null;
        }

        var tokenRequest = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["code"] = authorizationCode,
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            });

        using var response = await _httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            tokenRequest,
            cancellationToken);

        // A failed exchange should not be treated as a valid login.
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var tokenResponse =
            await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(
                cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(tokenResponse?.IdToken))
        {
            return null;
        }

        // Validate signature, issuer, expiry, and audience.
        var validationSettings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { _options.ClientId }
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(
            tokenResponse.IdToken,
            validationSettings);

        if (string.IsNullOrWhiteSpace(payload.Subject) ||
            string.IsNullOrWhiteSpace(payload.Email) ||
            payload.EmailVerified != true)
        {
            return null;
        }

        return new GoogleUserInfo(
            Subject: payload.Subject,
            Email: payload.Email,
            FirstName: payload.GivenName ?? string.Empty,
            LastName: payload.FamilyName ?? string.Empty,
            EmailVerified: payload.EmailVerified == true);
    }

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }
    }
}