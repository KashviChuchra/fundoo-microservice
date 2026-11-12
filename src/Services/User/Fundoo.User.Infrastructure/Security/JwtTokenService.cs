using Fundoo.User.Application.Interface;
using Fundoo.User.Domain;
using Fundoo.User.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Fundoo.User.Infrastructure.Security
{
    public class JwtTokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreateAccessToken(AppUser user)
        {
            string signingKey =
                _configuration["Jwt:SigningKey"]
                ?? throw new InvalidOperationException(
                    "JWT signing key is not configured.");

            string issuer =
                _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "JWT issuer is not configured.");

            string audience =
                _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "JWT audience is not configured.");

            if (Encoding.UTF8.GetByteCount(signingKey) < 32)
            {
                throw new InvalidOperationException(
                    "JWT signing key must be at least 32 bytes long.");
            }

            int expiryMinutes =
                _configuration.GetValue<int>("Jwt:ExpiryMinutes");

            if (expiryMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "JWT expiry must be greater than zero.");
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(signingKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                    new[]
                    {
                        new Claim(
                            JwtRegisteredClaimNames.Sub,
                            user.Id.ToString()),

                        new Claim(
                            JwtRegisteredClaimNames.Email,
                            user.Email)
                    }),

                Issuer = issuer,
                Audience = audience,
                IssuedAt = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
                SigningCredentials = credentials
            };

            var tokenHandler = new JsonWebTokenHandler();

            return tokenHandler.CreateToken(descriptor);
        }
    }
}