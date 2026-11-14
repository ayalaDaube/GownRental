using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Bl.Models;
using Microsoft.IdentityModel.Tokens;

namespace Server.Auth
{
    /// <summary>Short claim names used inside the token.</summary>
    public static class ClaimNames
    {
        public const string Name = "name";
        public const string Role = "role";
    }

    public interface ITokenService
    {
        /// <summary>Issues a signed token for the user and returns it with its expiry time (UTC).</summary>
        (string Token, DateTime ExpiresAtUtc) Create(AuthenticatedUser user);
    }

    public class TokenService : ITokenService
    {
        private readonly JwtSettings settings;
        private readonly SigningCredentials credentials;

        public TokenService(JwtSettings settings)
        {
            this.settings = settings;
            credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
                SecurityAlgorithms.HmacSha256);
        }

        public (string Token, DateTime ExpiresAtUtc) Create(AuthenticatedUser user)
        {
            var expires = DateTime.UtcNow.AddMinutes(settings.ExpiryMinutes);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimNames.Name, user.Username),
                new Claim(ClaimNames.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expires,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expires);
        }
    }
}
