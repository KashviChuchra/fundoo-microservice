using Fundoo.User.Application.Models;

namespace Fundoo.User.Application.Interface;

public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo?> ExchangeCodeAndValidateAsync(string authorizationCode, string redirectUri, CancellationToken cancellationToken);
}
