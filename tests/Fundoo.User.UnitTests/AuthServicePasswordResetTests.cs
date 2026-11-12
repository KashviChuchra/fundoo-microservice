using Fundoo.User.Application.Interface;
using Fundoo.User.Application.Services;
using Fundoo.User.Domain;
using Fundoo.User.Domain.Entities;
using Moq;
using Xunit;

namespace Fundoo.User.UnitTests
{
    public class AuthServicePasswordResetTests
    {
        private readonly Mock<IUserRepository> _userRepoMock = new();
        private readonly Mock<IPasswordHasher> _hasherMock = new();
        private readonly Mock<ITokenService> _tokenServiceMock = new();
        private readonly Mock<IMessagingClient> _messagingClientMock = new();

        private AuthService CreateService()
        {
            return new AuthService(
                _userRepoMock.Object,
                _hasherMock.Object,
                _tokenServiceMock.Object,
                _messagingClientMock.Object);
        }

        [Fact]
        public async Task ForgotPasswordAsync_EmptyEmail_ReturnsFailure()
        {
            var service = CreateService();

            var result = await service.ForgotPasswordAsync("", CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("Email is required.", result.Message);
        }

        [Fact]
        public async Task ForgotPasswordAsync_UserDoesNotExist_ReturnsSuccessToPreventEnumeration()
        {
            var service = CreateService();
            _userRepoMock
                .Setup(r => r.GetByNormalizedEmailAsync("UNKNOWN@EXAMPLE.COM", It.IsAny<CancellationToken>()))
                .ReturnsAsync((AppUser?)null);

            var result = await service.ForgotPasswordAsync("unknown@example.com", CancellationToken.None);

            Assert.True(result.Success);
            Assert.Contains("password-reset link has been sent", result.Message);
            _messagingClientMock.Verify(m => m.SendPasswordResetEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ForgotPasswordAsync_UserExistsAndActive_GeneratesTokenAndSendsEmail()
        {
            var service = CreateService();
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Doe",
                Email = "jane@example.com",
                NormalizedEmail = "JANE@EXAMPLE.COM",
                IsActive = true
            };

            _userRepoMock
                .Setup(r => r.GetByNormalizedEmailAsync("JANE@EXAMPLE.COM", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _hasherMock
                .Setup(h => h.Hash(It.IsAny<string>()))
                .Returns("hashed_token");

            var result = await service.ForgotPasswordAsync("jane@example.com", CancellationToken.None);

            Assert.True(result.Success);
            _userRepoMock.Verify(r => r.SaveResetTokenAsync(
                It.Is<PasswordResetToken>(t => t.UserId == user.Id && t.TokenHash.Length == 64),
                It.IsAny<CancellationToken>()), Times.Once);

            _messagingClientMock.Verify(m => m.SendPasswordResetEmailAsync(
                "Jane Doe",
                "jane@example.com",
                It.Is<string>(rawToken => rawToken.Length == 64),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("", "token", "Password@123")]
        [InlineData("test@example.com", "", "Password@123")]
        [InlineData("test@example.com", "token", "")]
        public async Task ResetPasswordAsync_MissingFields_ReturnsFailure(string email, string token, string newPassword)
        {
            var service = CreateService();

            var result = await service.ResetPasswordAsync(email, token, newPassword, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal("Email, token, and new password are required.", result.Message);
        }

        [Theory]
        [InlineData("short1!")]
        [InlineData("nouppercase1!")]
        [InlineData("NOLOWERCASE1!")]
        [InlineData("NoNumber!")]
        [InlineData("NoSpecial123")]
        public async Task ResetPasswordAsync_WeakPassword_ReturnsFailure(string weakPassword)
        {
            var service = CreateService();

            var result = await service.ResetPasswordAsync("test@example.com", "valid-token", weakPassword, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Contains("New password must be at least 8 characters", result.Message);
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidTokenAndUser_UpdatesPasswordAndMarksTokenUsed()
        {
            var service = CreateService();
            var userId = Guid.NewGuid();
            var user = new AppUser
            {
                Id = userId,
                Email = "jane@example.com",
                NormalizedEmail = "JANE@EXAMPLE.COM",
                IsActive = true
            };

            string rawToken = "raw_token";
            string expectedTokenHash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();

            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = expectedTokenHash,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
            };

            _userRepoMock
                .Setup(r => r.GetByNormalizedEmailAsync("JANE@EXAMPLE.COM", It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _hasherMock
                .Setup(h => h.Hash("NewSecurePassword@123"))
                .Returns("new_hashed_password");

            _userRepoMock
                .Setup(r => r.GetValidResetTokenAsync(expectedTokenHash, It.IsAny<CancellationToken>()))
                .ReturnsAsync(resetToken);

            var result = await service.ResetPasswordAsync("jane@example.com", rawToken, "NewSecurePassword@123", CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("Password has been reset successfully.", result.Message);

            _userRepoMock.Verify(r => r.UpdatePasswordAsync(userId, "new_hashed_password", It.IsAny<CancellationToken>()), Times.Once);
            _userRepoMock.Verify(r => r.MarkTokenUsedAsync(resetToken, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
