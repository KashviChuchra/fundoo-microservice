using Fundoo.User.Application.Interface;
using Fundoo.User.Application.Services;
using Fundoo.User.Domain;
using Fundoo.User.Domain.Entities;
using Moq;
using Xunit;

namespace Fundoo.User.UnitTests;

public class EmailOtpAuthenticationTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IMessagingClient> _messagingClient = new();

    [Fact]
    public async Task LoginAsync_SendsOtpAndReturnsChallengeWithoutToken()
    {
        var user = CreateUser();
        EmailOtpChallenge? savedChallenge = null;
        string? sentCode = null;
        _userRepository.Setup(r => r.GetByNormalizedEmailAsync(user.NormalizedEmail, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("Password@123", user.PasswordHash)).Returns(true);
        _userRepository.Setup(r => r.SaveEmailOtpChallengeAsync(It.IsAny<EmailOtpChallenge>(), It.IsAny<CancellationToken>()))
            .Callback<EmailOtpChallenge, CancellationToken>((challenge, _) => savedChallenge = challenge)
            .Returns(Task.CompletedTask);
        _messagingClient.Setup(m => m.SendLoginOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, code, _) => sentCode = code)
            .Returns(Task.CompletedTask);

        var result = await CreateService().LoginAsync(user.Email, "Password@123", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Null(result.Token);
        Assert.NotNull(result.ChallengeId);
        Assert.Equal(result.ChallengeId, savedChallenge?.Id);
        Assert.NotNull(sentCode);
        Assert.Equal(6, sentCode!.Length);
        _tokenService.Verify(t => t.CreateAccessToken(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task VerifyLoginOtpAsync_ValidCodeMarksChallengeUsedAndReturnsToken()
    {
        var user = CreateUser();
        var challenge = new EmailOtpChallenge
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CodeHash = Hash("123456"),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
        };
        _userRepository.Setup(r => r.GetValidEmailOtpChallengeAsync(challenge.Id, It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
        _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenService.Setup(t => t.CreateAccessToken(user)).Returns("jwt-token");

        var result = await CreateService().VerifyLoginOtpAsync(challenge.Id, "123456", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("jwt-token", result.Token);
        _userRepository.Verify(r => r.MarkEmailOtpChallengeUsedAsync(challenge, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyLoginOtpAsync_InvalidCodeIncrementsFailedAttemptsWithoutIssuingToken()
    {
        var challenge = new EmailOtpChallenge
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CodeHash = Hash("123456"),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
        };
        _userRepository.Setup(r => r.GetValidEmailOtpChallengeAsync(challenge.Id, It.IsAny<CancellationToken>())).ReturnsAsync(challenge);

        var result = await CreateService().VerifyLoginOtpAsync(challenge.Id, "654321", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(1, challenge.FailedAttempts);
        _userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _tokenService.Verify(t => t.CreateAccessToken(It.IsAny<AppUser>()), Times.Never);
    }

    private AuthService CreateService() => new(
        _userRepository.Object,
        _passwordHasher.Object,
        _tokenService.Object,
        _messagingClient.Object);

    private static AppUser CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Jane",
        LastName = "Doe",
        Email = "jane@example.com",
        NormalizedEmail = "JANE@EXAMPLE.COM",
        PasswordHash = "password-hash",
        IsActive = true
    };

    private static string Hash(string value) => Convert.ToHexString(
        System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
