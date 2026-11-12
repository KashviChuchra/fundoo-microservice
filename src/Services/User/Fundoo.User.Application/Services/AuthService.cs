using Fundoo.User.Application.Interface;
using Fundoo.User.Application.Models;
using Fundoo.User.Domain;
using Fundoo.User.Domain.Entities;

namespace Fundoo.User.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IMessagingClient _messagingClient;
        private readonly IGoogleTokenValidator? _googleTokenValidator;
        private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);
        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);

        public AuthService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            IMessagingClient messagingClient,
            IGoogleTokenValidator? googleTokenValidator = null)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _messagingClient = messagingClient;
            _googleTokenValidator = googleTokenValidator;
        }


        public async Task<AuthResult> RegisterAsync(string firstName,string lastName,string email,string password,CancellationToken cancellationToken)
        {
            firstName = firstName.Trim();
            lastName = lastName.Trim();
            email = email.Trim();

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return AuthResult.FailureResult("All fields are required.");
            }

            if (firstName.Length > 100 || lastName.Length > 100)
            {
                return AuthResult.FailureResult("First name and last name cannot exceed 100 characters.");
            }

            if (email.Length > 256 || !System.Net.Mail.MailAddress.TryCreate(email, out var address) ||
                !string.Equals(address.Address,email, StringComparison.OrdinalIgnoreCase))
            {
                return AuthResult.FailureResult("Please provide a valid email address.");
            }

            if (!IsValidPassword(password))
            {
                return AuthResult.FailureResult("Password must be at least 8 characters and contain an uppercase letter, a lowercase letter, a number, and a special character.");
            }

            string normalizedEmail = email.ToUpperInvariant();

            var existingUser = await _userRepository.GetByNormalizedEmailAsync(normalizedEmail,cancellationToken);

            if (existingUser != null)
            {
                return AuthResult.FailureResult("An account with this email already exists.");
            }

            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                NormalizedEmail = normalizedEmail,
                PasswordHash = _passwordHasher.Hash(password),
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            bool added = await _userRepository.AddAsync(user,cancellationToken);

            if (!added)
            {
                return AuthResult.FailureResult("An account with this email already exists.");
            }

            return AuthResult.SuccessResult("Registration successful.");
        }
       public async Task<AuthResult> LoginAsync(string email,string password, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return AuthResult.FailureResult("Invalid email or password.");
            }

            string normalizedEmail = email.Trim().ToUpperInvariant();

            var user = await _userRepository.GetByNormalizedEmailAsync(
                normalizedEmail,
                cancellationToken);

            if (user == null ||
                !_passwordHasher.Verify(password, user.PasswordHash))
            {
                return AuthResult.FailureResult(
                    "Invalid email or password.");
            }

            if (!user.IsActive)
            {
                return AuthResult.FailureResult(
                    "Invalid email or password.");
            }

            string code = GenerateOtpCode();
            var challenge = new EmailOtpChallenge
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CodeHash = HashToken(code),
                ExpiresAtUtc = DateTime.UtcNow.Add(OtpLifetime)
            };

            await _userRepository.SaveEmailOtpChallengeAsync(challenge, cancellationToken);

            try
            {
                await _messagingClient.SendLoginOtpEmailAsync(
                    $"{user.FirstName} {user.LastName}",
                    user.Email,
                    code,
                    cancellationToken);
            }
            catch (Exception)
            {
                return AuthResult.FailureResult("Failed to send the login verification code. Please try again.");
            }

            return AuthResult.SuccessResult(
                "A verification code has been sent to your email.",
                challengeId: challenge.Id);
        }

        public async Task<AuthResult> VerifyLoginOtpAsync(Guid challengeId, string code, CancellationToken cancellationToken)
        {
            if (challengeId == Guid.Empty || string.IsNullOrWhiteSpace(code))
            {
                return AuthResult.FailureResult("A verification code is required.");
            }

            var challenge = await _userRepository.GetValidEmailOtpChallengeAsync(challengeId, cancellationToken);
            if (challenge == null)
            {
                return AuthResult.FailureResult("Invalid or expired verification code.");
            }

            string suppliedHash = HashToken(code.Trim());
            bool matches = System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(suppliedHash),
                Convert.FromHexString(challenge.CodeHash));

            if (!matches)
            {
                challenge.FailedAttempts++;
                await _userRepository.SaveChangesAsync(cancellationToken);
                return AuthResult.FailureResult("Invalid or expired verification code.");
            }

            var user = await _userRepository.GetByIdAsync(challenge.UserId, cancellationToken);
            if (user == null || !user.IsActive)
            {
                return AuthResult.FailureResult("Invalid or expired verification code.");
            }

            await _userRepository.MarkEmailOtpChallengeUsedAsync(challenge, cancellationToken);
            return AuthResult.SuccessResult("Login successful.", _tokenService.CreateAccessToken(user));
        }

        public async Task<AuthResult> GoogleLoginAsync(string idToken, CancellationToken cancellationToken)
        {
            if (_googleTokenValidator == null)
            {
                throw new InvalidOperationException("Google token validation is not configured.");
            }

            var googleUser = await _googleTokenValidator.ValidateAsync(idToken, cancellationToken);
            if (googleUser == null)
            {
                return AuthResult.FailureResult("Invalid Google sign-in token.");
            }

            var user = await _userRepository.GetByGoogleSubjectAsync(googleUser.Subject, cancellationToken);
            if (user == null)
            {
                string normalizedEmail = googleUser.Email.Trim().ToUpperInvariant();
                user = await _userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

                if (user != null)
                {
                    if (!user.IsActive || (user.GoogleSubject != null && user.GoogleSubject != googleUser.Subject))
                    {
                        return AuthResult.FailureResult("Unable to link this Google account.");
                    }

                    user.GoogleSubject = googleUser.Subject;
                    await _userRepository.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    user = new AppUser
                    {
                        Id = Guid.NewGuid(),
                        FirstName = string.IsNullOrWhiteSpace(googleUser.FirstName) ? "Google" : googleUser.FirstName,
                        LastName = googleUser.LastName,
                        Email = googleUser.Email,
                        NormalizedEmail = normalizedEmail,
                        PasswordHash = _passwordHasher.Hash(GenerateSecureToken()),
                        GoogleSubject = googleUser.Subject,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow
                    };

                    if (!await _userRepository.AddAsync(user, cancellationToken))
                    {
                        return AuthResult.FailureResult("Unable to create the Google account.");
                    }
                }
            }

            if (!user.IsActive)
            {
                return AuthResult.FailureResult("Unable to sign in with this Google account.");
            }

            return AuthResult.SuccessResult("Google login successful.", _tokenService.CreateAccessToken(user));
        }
        public async Task<AuthResult> ForgotPasswordAsync(string email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))   return AuthResult.FailureResult("Email is required.");

            string normalizedEmail = email.Trim().ToUpperInvariant();

            var user = await _userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

            if (user == null || !user.IsActive) return AuthResult.SuccessResult("If an account with that email exists, a password-reset link has been sent.");

            // Generate a cryptographically random token (32 bytes → 64 hex chars)
            string rawToken = GenerateSecureToken();
            string tokenHash = HashToken(rawToken);

            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAtUtc = DateTime.UtcNow.Add(TokenLifetime)
            };

            await _userRepository.SaveResetTokenAsync(resetToken, cancellationToken);

            try
            {
                await _messagingClient.SendPasswordResetEmailAsync(
                    $"{user.FirstName} {user.LastName}",
                    user.Email,
                    rawToken,
                    cancellationToken);
            }

            catch (Exception)
            {
                return AuthResult.FailureResult("Failed to send password-reset email. Please try again.");
            }

            return AuthResult.SuccessResult("If an account with that email exists, a password-reset link has been sent.");
        }
        public async Task<AuthResult> ResetPasswordAsync(string email,string token,string newPassword,CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(token) ||
                string.IsNullOrWhiteSpace(newPassword))
            {
                return AuthResult.FailureResult("Email, token, and new password are required.");
            }

            if (!IsValidPassword(newPassword))
            {
                return AuthResult.FailureResult(
                    "New password must be at least 8 characters and contain an uppercase letter, a lowercase letter, a number, and a special character.");
            }

            string normalizedEmail = email.Trim().ToUpperInvariant();

            var user = await _userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

            if (user == null || !user.IsActive)return AuthResult.FailureResult("Invalid or expired reset token.");

            var storedToken = await FindValidTokenForUserAsync(user.Id, token, cancellationToken);

            if (storedToken == null)    return AuthResult.FailureResult("Invalid or expired reset token.");

            string newHash = _passwordHasher.Hash(newPassword);

            await _userRepository.UpdatePasswordAsync(user.Id, newHash, cancellationToken);
            await _userRepository.MarkTokenUsedAsync(storedToken, cancellationToken);

            return AuthResult.SuccessResult("Password has been reset successfully.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task<PasswordResetToken?> FindValidTokenForUserAsync(
            Guid userId,
            string rawToken,
            CancellationToken cancellationToken)
        {
            string tokenHash = HashToken(rawToken);
            var stored = await _userRepository.GetValidResetTokenAsync(tokenHash, cancellationToken);
            if (stored == null || stored.UserId != userId) return null;
            return stored;
        }

        private static string HashToken(string token)
        {
            byte[] hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string GenerateSecureToken()
        {
            var bytes = new byte[32];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string GenerateOtpCode()
        {
            return System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        }

        private static bool IsValidPassword(string password)
        {
            return password.Length >= 8
                && password.Any(char.IsUpper)
                && password.Any(char.IsLower)
                && password.Any(char.IsDigit)
                && password.Any(c => !char.IsLetterOrDigit(c));
        }
    }
}