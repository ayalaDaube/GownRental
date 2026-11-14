using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Bl.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Server.Auth;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IBLAuth auth;
        private readonly ITokenService tokens;

        public AuthController(IBl bl, ITokenService tokens)
        {
            auth = bl.BLAuth;
            this.tokens = tokens;
        }

        /// <summary>Signs in with a username and password and returns a JWT to send on later requests.</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
        {
            var user = auth.ValidateCredentials(request.Username, request.Password);
            if (user == null)
            {
                // One message for both unknown user and wrong password, so attackers learn nothing.
                return Unauthorized(new { error = "Invalid username or password." });
            }

            var (token, expiresAtUtc) = tokens.Create(user);
            return Ok(new LoginResponse(token, expiresAtUtc, user.Username, user.Role));
        }

        /// <summary>Returns who the current token belongs to; the client uses it to check a stored token is still valid.</summary>
        [HttpGet("me")]
        public ActionResult<CurrentUser> Me()
        {
            var username = User.FindFirstValue(ClaimNames.Name) ?? string.Empty;
            var role = User.FindFirstValue(ClaimNames.Role) ?? string.Empty;
            return Ok(new CurrentUser(username, role));
        }
    }

    public class LoginRequest
    {
        [Required, StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Password { get; set; } = string.Empty;
    }

    public record LoginResponse(string Token, DateTime ExpiresAtUtc, string Username, string Role);

    public record CurrentUser(string Username, string Role);
}
