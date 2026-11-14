using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Dal.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Server.Auth;
using Server.Controllers;

namespace Server.Tests;

public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory factory;

    public AuthTests(ApiFactory factory)
    {
        this.factory = factory;
    }

    private HttpClient NewClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static HttpRequestMessage Get(string url, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<string> LoginAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            username = ApiFactory.SecretaryUsername,
            password = ApiFactory.SecretaryPassword,
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    private static string CreateToken(string role, DateTime expiresUtc, string key = ApiFactory.SigningKey)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "GownRental",
            audience: "GownRental.Client",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "1"),
                new Claim(ClaimNames.Name, "someone"),
                new Claim(ClaimNames.Role, role),
            },
            notBefore: expiresUtc.AddHours(-1),
            expires: expiresUtc,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Theory]
    [InlineData("/api/Gowns")]
    [InlineData("/api/Gowns/groups")]
    [InlineData("/api/Rentals")]
    [InlineData("/api/Appointment")]
    [InlineData("/api/Auth/me")]
    public async Task Protected_endpoints_reject_requests_without_a_token(string url)
    {
        var response = await NewClient().SendAsync(Get(url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Visitors_can_list_free_appointment_slots_without_signing_in()
    {
        var response = await NewClient().SendAsync(Get("/api/Appointment/getAvailableAppointments"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_correct_credentials_returns_a_token_that_opens_protected_endpoints()
    {
        var client = NewClient();

        var token = await LoginAsync(client);
        var gowns = await client.SendAsync(Get("/api/Gowns", token));
        var me = await client.SendAsync(Get("/api/Auth/me", token));

        Assert.Equal(HttpStatusCode.OK, gowns.StatusCode);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var current = await me.Content.ReadFromJsonAsync<CurrentUser>();
        Assert.Equal(ApiFactory.SecretaryUsername, current!.Username);
        Assert.Equal("Secretary", current.Role);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_is_rejected()
    {
        var response = await NewClient().PostAsJsonAsync("/api/Auth/login",
            new { username = ApiFactory.SecretaryUsername, password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_an_unknown_user_gives_the_same_answer_as_a_wrong_password()
    {
        var client = NewClient();

        var unknown = await client.PostAsJsonAsync("/api/Auth/login", new { username = "nobody", password = "whatever-123" });
        var wrong = await client.PostAsJsonAsync("/api/Auth/login",
            new { username = ApiFactory.SecretaryUsername, password = "whatever-123" });

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(await wrong.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_without_credentials_is_a_bad_request()
    {
        var response = await NewClient().PostAsJsonAsync("/api/Auth/login", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_modified_token_is_rejected()
    {
        var client = NewClient();
        var token = await LoginAsync(client);

        var response = await client.SendAsync(Get("/api/Gowns", token + "x"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task An_expired_token_is_rejected()
    {
        var token = CreateToken("Secretary", DateTime.UtcNow.AddMinutes(-30));

        var response = await NewClient().SendAsync(Get("/api/Gowns", token));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_signed_with_a_different_key_is_rejected()
    {
        var token = CreateToken("Secretary", DateTime.UtcNow.AddHours(1),
            key: "another-signing-key-another-signing-key-0123456789");

        var response = await NewClient().SendAsync(Get("/api/Gowns", token));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_valid_token_without_the_secretary_role_is_forbidden()
    {
        var token = CreateToken("Customer", DateTime.UtcNow.AddHours(1));

        var response = await NewClient().SendAsync(Get("/api/Gowns", token));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public void Passwords_are_stored_as_hashes_not_as_text()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataManager>();

        var user = db.AppUsers.Single(u => u.Username == ApiFactory.SecretaryUsername);

        Assert.NotEqual(ApiFactory.SecretaryPassword, user.PasswordHash);
        Assert.DoesNotContain(ApiFactory.SecretaryPassword, user.PasswordHash);
    }
}
