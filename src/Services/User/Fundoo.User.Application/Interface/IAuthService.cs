using System;
using System.Collections.Generic;
using System.Text;
using Fundoo.User.Application.Models;
namespace Fundoo.User.Application.Interface;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string firstName,string lastName,string email, string password,CancellationToken cancellationToken);
    Task<AuthResult> LoginAsync(string email,string password,CancellationToken cancellationToken);
    Task<AuthResult> VerifyLoginOtpAsync(Guid challengeId, string code, CancellationToken cancellationToken);
    Task<AuthResult> GoogleLoginAsync(string idToken, CancellationToken cancellationToken);
    Task<AuthResult> ForgotPasswordAsync(string email, CancellationToken cancellationToken);
    Task<AuthResult> ResetPasswordAsync(string email,string token, string newPassword, CancellationToken cancellationToken);
}


