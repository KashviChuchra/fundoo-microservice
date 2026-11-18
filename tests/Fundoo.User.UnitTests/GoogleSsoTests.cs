using Fundoo.User.Application.Interface;
using Fundoo.User.Application.Models;
using Fundoo.User.Application.Services;
using Fundoo.User.Domain.Entities;
using Moq;
using Xunit;

namespace Fundoo.User.UnitTests;

public sealed class GoogleSsoTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IMessagingClient> _messagingClient = new();
    private readonly Mock<IGoogleTokenValidator> _googleValidator = new();

    [Fact]
    public async Task GoogleLoginAsync_ExistingEmailLinksGoogleSubjectAndReturnsJwt()
    {
        var user = CreateUser();
        var googleUser = new GoogleUserInfo("google-subject", user.Email, "Jane", "Doe");
        _googleValidator.Setup(v => v.ExchangeCodeAndValidateAsync("code", "https://client.example/callback", It.IsAny<CancellationToken>())).ReturnsAsync(googleUser);
        _userRepository.Setup(r => r.GetByGoogleSubjectAsync(googleUser.Subject, It.IsAny<CancellationToken>())).ReturnsAsync((AppUser?)null);
        _userRepository.Setup(r => r.GetByNormalizedEmailAsync(user.NormalizedEmail, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _tokenService.Setup(t => t.CreateAccessToken(user)).Returns("jwt");

        var result = await CreateService().GoogleLoginAsync("code", "https://client.example/callback", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("jwt", result.Token);
        Assert.Equal(googleUser.Subject, user.GoogleSubject);
        _userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GoogleLoginAsync_NewEmailProvisionsUser()
    {
        var googleUser = new GoogleUserInfo("new-subject", "new@example.com", "New", "User");
        AppUser? addedUser = null;
        _googleValidator.Setup(v => v.ExchangeCodeAndValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(googleUser);
        _userRepository.Setup(r => r.GetByGoogleSubjectAsync(googleUser.Subject, It.IsAny<CancellationToken>())).ReturnsAsync((AppUser?)null);
        _userRepository.Setup(r => r.GetByNormalizedEmailAsync("NEW@EXAMPLE.COM", It.IsAny<CancellationToken>())).ReturnsAsync((AppUser?)null);
        _userRepository.Setup(r => r.AddAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .Callback<AppUser, CancellationToken>((user, _) => addedUser = user)
            .ReturnsAsync(true);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("unusable-password-hash");

        var result = await CreateService().GoogleLoginAsync("code", "https://client.example/callback", CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(addedUser);
        Assert.Equal(googleUser.Subject, addedUser!.GoogleSubject);
        Assert.Equal("NEW@EXAMPLE.COM", addedUser.NormalizedEmail);
    }

    [Fact]
    public async Task GoogleLoginAsync_ConflictingLinkedSubjectIsRejected()
    {
        var user = CreateUser();
        user.GoogleSubject = "different-subject";
        var googleUser = new GoogleUserInfo("incoming-subject", user.Email, "Jane", "Doe");
        _googleValidator.Setup(v => v.ExchangeCodeAndValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(googleUser);
        _userRepository.Setup(r => r.GetByGoogleSubjectAsync(googleUser.Subject, It.IsAny<CancellationToken>())).ReturnsAsync((AppUser?)null);
        _userRepository.Setup(r => r.GetByNormalizedEmailAsync(user.NormalizedEmail, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateService().GoogleLoginAsync("code", "https://client.example/callback", CancellationToken.None);

        Assert.False(result.Success);
        _tokenService.Verify(t => t.CreateAccessToken(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task GoogleLoginAsync_InvalidGoogleResponseDoesNotAccessRepository()
    {
        _googleValidator.Setup(v => v.ExchangeCodeAndValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((GoogleUserInfo?)null);

        var result = await CreateService().GoogleLoginAsync("invalid-code", "https://client.example/callback", CancellationToken.None);

        Assert.False(result.Success);
        _userRepository.Verify(r => r.GetByGoogleSubjectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private AuthService CreateService() => new(
        _userRepository.Object,
        _passwordHasher.Object,
        _tokenService.Object,
        _messagingClient.Object,
        _googleValidator.Object);

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
}
