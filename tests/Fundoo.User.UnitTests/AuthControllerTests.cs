using Fundoo.User.Api.Contracts;
using Fundoo.User.Api.Controllers;
using Fundoo.User.Application.Interface;
using Fundoo.User.Application.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Fundoo.User.UnitTests
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _authServiceMock = new();

        [Fact]
        public async Task ForgotPassword_ReturnsOk_WhenServiceSucceeds()
        {
            var controller = new AuthController(_authServiceMock.Object);
            var request = new ForgotPasswordRequest { Email = "user@example.com" };

            _authServiceMock
                .Setup(s => s.ForgotPasswordAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AuthResult.SuccessResult("Reset link sent."));

            var actionResult = await controller.ForgotPassword(request, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<AuthResponse>(okResult.Value);
            Assert.Equal("Reset link sent.", response.Message);
        }

        [Fact]
        public async Task ForgotPassword_ReturnsBadRequest_WhenServiceFails()
        {
            var controller = new AuthController(_authServiceMock.Object);
            var request = new ForgotPasswordRequest { Email = "" };

            _authServiceMock
                .Setup(s => s.ForgotPasswordAsync(request.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AuthResult.FailureResult("Email is required."));

            var actionResult = await controller.ForgotPassword(request, CancellationToken.None);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            var response = Assert.IsType<AuthResponse>(badRequestResult.Value);
            Assert.Equal("Email is required.", response.Message);
        }

        [Fact]
        public async Task ResetPassword_ReturnsOk_WhenServiceSucceeds()
        {
            var controller = new AuthController(_authServiceMock.Object);
            var request = new ResetPasswordRequest
            {
                Email = "user@example.com",
                Token = "valid-token",
                NewPassword = "Password@123"
            };

            _authServiceMock
                .Setup(s => s.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AuthResult.SuccessResult("Password has been reset successfully."));

            var actionResult = await controller.ResetPassword(request, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<AuthResponse>(okResult.Value);
            Assert.Equal("Password has been reset successfully.", response.Message);
        }

        [Fact]
        public async Task ResetPassword_ReturnsBadRequest_WhenServiceFails()
        {
            var controller = new AuthController(_authServiceMock.Object);
            var request = new ResetPasswordRequest
            {
                Email = "user@example.com",
                Token = "expired-token",
                NewPassword = "Password@123"
            };

            _authServiceMock
                .Setup(s => s.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AuthResult.FailureResult("Invalid or expired reset token."));

            var actionResult = await controller.ResetPassword(request, CancellationToken.None);

            var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
            var response = Assert.IsType<AuthResponse>(badRequestResult.Value);
            Assert.Equal("Invalid or expired reset token.", response.Message);
        }
    }
}
