namespace Fundoo.User.Application.Models;

public class AuthResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public Guid? ChallengeId { get; set; }

    public static AuthResult SuccessResult(string message, string? token = null, Guid? challengeId = null)
    {
        return new AuthResult
        {
            Success = true,
            Message = message,
            Token = token,
            ChallengeId = challengeId
        };
    }

    public static AuthResult FailureResult(string message)
    {
        return new AuthResult
        {
            Success = false,
            Message = message
        };
    }
}
