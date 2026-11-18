namespace Fundoo.User.Domain.Entities;

public class EmailOtpChallenge
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? UsedAtUtc { get; set; }
}
