using Fundoo.User.Domain;
using Fundoo.User.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fundoo.User.Infrastructure.Persistence
{
    public class UserDbContext : DbContext
    {
        public UserDbContext(DbContextOptions<UserDbContext> options): base(options)
        {
        }

        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
        public DbSet<EmailOtpChallenge> EmailOtpChallenges => Set<EmailOtpChallenge>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.HasKey(user => user.Id);

                entity.HasIndex(user => user.NormalizedEmail)
                    .IsUnique();

                entity.Property(user => user.FirstName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(user => user.LastName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(user => user.Email)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(user => user.NormalizedEmail)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(user => user.PasswordHash)
                    .IsRequired();

                entity.Property(user => user.GoogleSubject)
                    .HasMaxLength(255);

                entity.HasIndex(user => user.GoogleSubject)
                    .IsUnique();

                entity.Property(user => user.IsActive)
                    .IsRequired();

                entity.Property(user => user.CreatedAtUtc)
                    .IsRequired();

                entity.Property(user => user.UpdatedAtUtc)
                    .IsRequired();
            });

            modelBuilder.Entity<EmailOtpChallenge>(entity =>
            {
                entity.HasKey(challenge => challenge.Id);

                entity.Property(challenge => challenge.CodeHash)
                    .IsRequired();

                entity.HasIndex(challenge => challenge.UserId);
                entity.HasIndex(challenge => challenge.ExpiresAtUtc);
            });

            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.HasKey(token => token.Id);

                entity.Property(token => token.TokenHash)
                    .IsRequired();

                entity.HasIndex(token => token.UserId);

                entity.HasIndex(token => token.ExpiresAtUtc);
            });
        }
    }
}