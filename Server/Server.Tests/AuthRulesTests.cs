using System.Net;
using System.Net.Http.Json;
using Bl.Api;
using Bl.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Server.Tests;

/// <summary>What counts as a correct or an incorrect sign-in, from the point of view of the person signing in.</summary>
public class AuthRulesTests
{
    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string? username, string? password) =>
        client.PostAsJsonAsync("/api/Auth/login", new { username, password });

    [Fact]
    public async Task The_correct_username_and_password_sign_in()
    {
        using var app = new ApiFactory();

        var response = await LoginAsync(app.CreateClient(), ApiFactory.SecretaryUsername, ApiFactory.SecretaryPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("TEST-SECRETARY")]
    [InlineData("Test-Secretary")]
    [InlineData("  test-secretary  ")]
    public async Task A_username_is_not_case_sensitive_and_spaces_around_it_do_not_matter(string typed)
    {
        using var app = new ApiFactory();

        var response = await LoginAsync(app.CreateClient(), typed, ApiFactory.SecretaryPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("test-password-12345")]            // different case
    [InlineData("Test-Password-12345 ")]           // trailing space
    [InlineData(" Test-Password-12345")]           // leading space
    [InlineData("Test-Password-1234")]             // one character short
    [InlineData("Test-Password-123456")]           // one character more
    public async Task A_password_must_match_exactly(string typed)
    {
        using var app = new ApiFactory();

        var response = await LoginAsync(app.CreateClient(), ApiFactory.SecretaryUsername, typed);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(null, "x")]
    [InlineData("x", null)]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public async Task A_missing_or_empty_username_or_password_is_a_bad_request_or_unauthorized_never_an_error(string? username, string? password)
    {
        using var app = new ApiFactory();

        var response = await LoginAsync(app.CreateClient(), username, password);

        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized, response.StatusCode.ToString());
    }

    [Fact]
    public async Task Very_long_usernames_and_passwords_are_refused_without_an_error()
    {
        using var app = new ApiFactory();

        var longName = await LoginAsync(app.CreateClient(), new string('a', 5000), "x");
        var longPassword = await LoginAsync(app.CreateClient(), ApiFactory.SecretaryUsername, new string('p', 100_000));

        Assert.Equal(HttpStatusCode.BadRequest, longName.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, longPassword.StatusCode);
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("test-secretary'--")]
    [InlineData("\" OR \"\"=\"")]
    public async Task Injection_style_text_is_just_a_wrong_name(string username)
    {
        using var app = new ApiFactory();

        var response = await LoginAsync(app.CreateClient(), username, "' OR '1'='1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Passwords_in_Hebrew_and_with_symbols_work()
    {
        using var app = new ApiFactory();
        using (var scope = app.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IBl>().BLAuth.EnsureUser("dana", "סיסמה-ארוכה-123!@#", Roles.Secretary);
        }

        var right = await LoginAsync(app.CreateClient(), "dana", "סיסמה-ארוכה-123!@#");
        var wrong = await LoginAsync(app.CreateClient(), "dana", "סיסמה-ארוכה-123!@$");

        Assert.Equal(HttpStatusCode.OK, right.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
    }

    [Fact]
    public void Creating_an_account_that_exists_never_changes_its_password()
    {
        using var app = new ApiFactory();
        using var scope = app.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IBl>().BLAuth;

        auth.EnsureUser(ApiFactory.SecretaryUsername, "A-completely-new-password-1", Roles.Secretary);

        Assert.NotNull(auth.ValidateCredentials(ApiFactory.SecretaryUsername, ApiFactory.SecretaryPassword));
        Assert.Null(auth.ValidateCredentials(ApiFactory.SecretaryUsername, "A-completely-new-password-1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("1234567")]
    public void An_account_cannot_be_created_with_a_weak_password(string password)
    {
        using var app = new ApiFactory();
        using var scope = app.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IBl>().BLAuth;

        Assert.ThrowsAny<ArgumentException>(() => auth.EnsureUser("newperson", password, Roles.Secretary));
        Assert.Null(auth.ValidateCredentials("newperson", password));
    }

    [Fact]
    public void Two_accounts_whose_names_differ_only_in_case_are_the_same_account()
    {
        using var app = new ApiFactory();
        using var scope = app.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IBl>().BLAuth;

        auth.EnsureUser("TEST-SECRETARY", "Another-password-123", Roles.Secretary);

        Assert.Null(auth.ValidateCredentials("test-secretary", "Another-password-123"));
        Assert.NotNull(auth.ValidateCredentials("test-secretary", ApiFactory.SecretaryPassword));
    }

    // ---- guessing passwords

    private class LimitedFactory : ApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimit:LoginPerMinute"] = "3" }));
        }
    }

    [Fact]
    public async Task After_too_many_attempts_even_the_right_password_is_refused_for_a_while()
    {
        using var app = new LimitedFactory();
        var client = app.CreateClient();

        var first = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
        {
            first.Add((await LoginAsync(client, ApiFactory.SecretaryUsername, "wrong-" + i)).StatusCode);
        }
        var afterLimit = await LoginAsync(client, ApiFactory.SecretaryUsername, "wrong-again");
        var rightButLate = await LoginAsync(client, ApiFactory.SecretaryUsername, ApiFactory.SecretaryPassword);

        Assert.All(first, status => Assert.Equal(HttpStatusCode.Unauthorized, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, afterLimit.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, rightButLate.StatusCode);
    }

    [Fact]
    public async Task The_attempt_limit_applies_to_signing_in_only()
    {
        using var app = new LimitedFactory();
        var client = await app.SignedInClientAsync();
        for (var i = 0; i < 6; i++)
        {
            await LoginAsync(app.CreateClient(), ApiFactory.SecretaryUsername, "wrong");
        }

        var response = await client.GetAsync("/api/Gowns");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
