using Fundoo.User.Domain;
using Fundoo.User.Domain.Entities;

namespace Fundoo.User.Application.Interface
{
    public interface IUserRepository
    {
        Task<AppUser?> GetByNormalizedEmailAsync(string normalizedEmail,CancellationToken cancellationToken);
        Task<AppUser?> GetByIdAsync(Guid id,CancellationToken cancellationToken);
        Task<bool> AddAsync(AppUser user,CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);


        // ── Password-reset token methods ──────────────────────────────────────

        Task SaveResetTokenAsync(PasswordResetToken token,CancellationToken cancellationToken);
        Task<PasswordResetToken?> GetValidResetTokenAsync(string tokenHash,CancellationToken cancellationToken);
        Task MarkTokenUsedAsync(PasswordResetToken token,CancellationToken cancellationToken);
        Task UpdatePasswordAsync(Guid userId,string newPasswordHash,CancellationToken cancellationToken);
        Task SaveEmailOtpChallengeAsync(EmailOtpChallenge challenge, CancellationToken cancellationToken);
        Task<EmailOtpChallenge?> GetValidEmailOtpChallengeAsync(Guid challengeId, CancellationToken cancellationToken);
        Task MarkEmailOtpChallengeUsedAsync(EmailOtpChallenge challenge, CancellationToken cancellationToken);
        Task<AppUser?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken);
    }
}