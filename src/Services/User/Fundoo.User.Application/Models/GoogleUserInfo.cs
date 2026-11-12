namespace Fundoo.User.Application.Models;

public sealed record GoogleUserInfo(
    string Subject,
    string Email,
    string FirstName,
    string LastName);
