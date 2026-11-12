using Fundoo.User.Application.Models;

namespace Fundoo.User.Application.Interface;

public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken);
}
