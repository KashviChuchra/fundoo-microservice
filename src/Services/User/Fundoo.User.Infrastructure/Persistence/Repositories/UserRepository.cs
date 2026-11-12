using Fundoo.User.Application.Interface;
using Fundoo.User.Domain;
using Fundoo.User.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fundoo.User.Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserDbContext _context;
        public UserRepository(UserDbContext context)
        {
            _context = context;
        }

        public async Task<AppUser?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken)
        {
            return await _context.Users.FirstOrDefaultAsync(user => user.GoogleSubject == googleSubject, cancellationToken);
        }

        public async Task<AppUser?> GetByNormalizedEmailAsync(string normalizedEmail,CancellationToken cancellationToken)
        {
            return await _context.Users.FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail,cancellationToken);
        }

        public async Task<AppUser?> GetByIdAsync(Guid id,CancellationToken cancellationToken)
        {
            return await _context.Users.FirstOrDefaultAsync(user => user.Id == id,cancellationToken);
        }

        public async Task<bool> AddAsync(AppUser user,CancellationToken cancellationToken)
        {
            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException)
            {
                _context.Entry(user).State = EntityState.Detached;
                return false;
            }
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task SaveEmailOtpChallengeAsync(EmailOtpChallenge challenge, CancellationToken cancellationToken)
        {
            _context.EmailOtpChallenges.Add(challenge);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<EmailOtpChallenge?> GetValidEmailOtpChallengeAsync(Guid challengeId, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            return await _context.EmailOtpChallenges
                .FirstOrDefaultAsync(challenge => challenge.Id == challengeId
                    && challenge.ExpiresAtUtc > now
                    && challenge.UsedAtUtc == null
                    && challenge.FailedAttempts < 5, cancellationToken);
        }

        public async Task MarkEmailOtpChallengeUsedAsync(EmailOtpChallenge challenge, CancellationToken cancellationToken)
        {
            challenge.UsedAtUtc = DateTime.UtcNow;
            _context.EmailOtpChallenges.Update(challenge);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // ── Password-reset token methods ──────────────────────────────────────

        public async Task SaveResetTokenAsync(PasswordResetToken token,CancellationToken cancellationToken)
        {
            _context.PasswordResetTokens.Add(token);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<PasswordResetToken?> GetValidResetTokenAsync(string tokenHash,CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            return await _context.PasswordResetTokens
                .FirstOrDefaultAsync(
                    t => t.TokenHash == tokenHash
                      && t.ExpiresAtUtc > now
                      && t.UsedAtUtc == null,
                    cancellationToken);
        }

        public async Task MarkTokenUsedAsync(PasswordResetToken token,CancellationToken cancellationToken)
        {
            token.UsedAtUtc = DateTime.UtcNow;
            _context.PasswordResetTokens.Update(token);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdatePasswordAsync(Guid userId,string newPasswordHash,CancellationToken cancellationToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is null) return;

            user.PasswordHash = newPasswordHash;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}