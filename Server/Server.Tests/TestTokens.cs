using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Server.Auth;

namespace Server.Tests;

/// <summary>Builds tokens the way an attacker or a buggy client might, to see which ones the API accepts.</summary>
public static class TestTokens
{
    public const string Issuer = "GownRental";
    public const string Audience = "GownRental.Client";

    public static string Create(
        string role = "Secretary",
        DateTime? expiresUtc = null,
        DateTime? notBeforeUtc = null,
        string key = ApiFactory.SigningKey,
        string issuer = Issuer,
        string audience = Audience,
        string algorithm = SecurityAlgorithms.HmacSha256,
        bool includeExpiry = true)
    {
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), algorithm);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "1"),
                new Claim(ClaimNames.Name, "someone"),
                new Claim(ClaimNames.Role, role),
            },
            notBefore: notBeforeUtc ?? DateTime.UtcNow.AddMinutes(-5),
            expires: includeExpiry ? expiresUtc ?? DateTime.UtcNow.AddHours(1) : null,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>A token that claims to be signed with the "none" algorithm, i.e. not signed at all.</summary>
    public static string Unsigned(string role = "Secretary")
    {
        static string Base64Url(string json) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var header = Base64Url("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = Base64Url(
            $"{{\"sub\":\"1\",\"name\":\"someone\",\"role\":\"{role}\",\"iss\":\"{Issuer}\",\"aud\":\"{Audience}\",\"exp\":{exp}}}");
        return $"{header}.{payload}.";
    }
}
