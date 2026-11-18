using Fundoo.User.Application.Interface;
using Fundoo.User.Application.Models;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Json;

namespace Fundoo.User.Infrastructure.Security;

public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public GoogleTokenValidator(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<GoogleUserInfo?> ExchangeCodeAndValidateAsync(string authorizationCode, string redirectUri, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authorizationCode) || string.IsNullOrWhiteSpace(redirectUri) || cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        string clientId = _configuration["Google:ClientId"] ?? throw new InvalidOperationException("Google:ClientId is not configured.");
        string clientSecret = _configuration["Google:ClientSecret"] ?? throw new InvalidOperationException("Google:ClientSecret is not configured.");
        string configuredRedirectUri = _configuration["Google:RedirectUri"] ?? throw new InvalidOperationException("Google:RedirectUri is not configured.");

        if (!string.Equals(redirectUri, configuredRedirectUri, StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = authorizationCode,
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret,
                    ["redirect_uri"] = redirectUri,
                    ["grant_type"] = "authorization_code"
                })
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken);
            if (string.IsNullOrWhiteSpace(tokenResponse?.IdToken))
            {
                return null;
            }

            var payload = await GoogleJsonWebSignature.ValidateAsync(
                tokenResponse.IdToken,
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
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }
    }
}
