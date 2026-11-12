using Fundoo.User.Api.Contracts;
using Fundoo.User.Application.Interface;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace Fundoo.User.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IValidator<RegisterRequest>? _registerValidator;

        public AuthController(IAuthService authService, IValidator<RegisterRequest>? registerValidator = null)
        {
            _authService = authService;
            _registerValidator = registerValidator;
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.GoogleLoginAsync(request.IdToken, cancellationToken);

            if (!result.Success)
            {
                return Unauthorized(new AuthResponse { Message = result.Message });
            }

            return Ok(new AuthResponse
            {
                Message = result.Message,
                Token = result.Token
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register( [FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            var validationResult = _registerValidator is null
                ? new FluentValidation.Results.ValidationResult()
                : await _registerValidator.ValidateAsync(request,cancellationToken);
            // for fluent error handling, ValidateAsync is built in function
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(error => new
                    {
                        Field = error.PropertyName,
                        Message = error.ErrorMessage
                    });

                return BadRequest(new
                {
                    Message = "Validation failed.",
                    Errors = errors
                });
            }
            var result = await _authService.RegisterAsync(request.FirstName, request.LastName, request.Email,request.Password,cancellationToken);

            if (!result.Success)
            {
                if (result.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
                {
                    return Conflict(new AuthResponse
                    {
                        Message = result.Message
                    });
                }

                return BadRequest(new AuthResponse
                {
                    Message = result.Message
                });
            }

            return StatusCode( StatusCodes.Status201Created,
                new AuthResponse
                {
                    Message = result.Message
                });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request,CancellationToken cancellationToken)
        {
            var result = await _authService.LoginAsync(request.Email,request.Password,cancellationToken);

            if (!result.Success)
            {
                return Unauthorized(new AuthResponse
                {
                    Message = result.Message
                });
            }

            return Ok(new AuthResponse
            {
                Message = result.Message,
                Token = result.Token,
                ChallengeId = result.ChallengeId
            });
        }

        [HttpPost("verify-login-otp")]
        public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyLoginOtpRequest request, CancellationToken cancellationToken)
        {
            var result = await _authService.VerifyLoginOtpAsync(request.ChallengeId, request.Code, cancellationToken);

            if (!result.Success)
            {
                return Unauthorized(new AuthResponse { Message = result.Message });
            }

            return Ok(new AuthResponse
            {
                Message = result.Message,
                Token = result.Token
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request,CancellationToken cancellationToken)
        {
            var result = await _authService.ForgotPasswordAsync(request.Email,cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new AuthResponse
                {
                    Message = result.Message
                });
            }

            return Ok(new AuthResponse
            {
                Message = result.Message
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request,CancellationToken cancellationToken)
        {
            var result = await _authService.ResetPasswordAsync(
                request.Email,
                request.Token,
                request.NewPassword,
                cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new AuthResponse
                {
                    Message = result.Message
                });
            }

            return Ok(new AuthResponse
            {
                Message = result.Message
            });
        }
    }
}