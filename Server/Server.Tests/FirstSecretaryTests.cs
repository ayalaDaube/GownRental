using System.Net;
using System.Net.Http.Json;
using Bl.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Server.Startup;

namespace Server.Tests;

/// <summary>
/// A new installation must be able to get its first secretary without a password written in a file,
/// and a restart must never change an existing account.
/// </summary>
public class FirstSecretaryTests
{
    private const string Password = "a-strong-first-pass";

    private static IConfiguration Settings(string? username, string? password) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Bootstrap:Username"] = username,
            ["Bootstrap:Password"] = password,
        }).Build();

    /// <summary>An installation with no accounts at all.</summary>
    private static ApiFactory NewInstallation()
    {
        var app = new ApiFactory();
        app.Query(db => { db.AppUsers.RemoveRange(db.AppUsers); db.SaveChanges(); return 0; });
        return app;
    }

    private static bool Run(ApiFactory app, IConfiguration settings)
    {
        using var scope = app.Services.CreateScope();
        return FirstSecretary.CreateIfNeeded(scope.ServiceProvider.GetRequiredService<IBl>().BLAuth, settings);
    }

    private static Task<HttpResponseMessage> SignIn(ApiFactory app, string username, string password) =>
        app.CreateClient().PostAsJsonAsync("/api/Auth/login", new { username, password });

    [Fact]
    public async Task On_a_new_installation_the_configured_secretary_is_created_and_can_sign_in()
    {
        using var app = NewInstallation();

        Assert.True(Run(app, Settings("Owner", Password)));

        Assert.Equal(HttpStatusCode.OK, (await SignIn(app, "owner", Password)).StatusCode);
        Assert.Equal(1, app.Query(db => db.AppUsers.Count()));
    }

    [Fact]
    public void Without_settings_nothing_is_created()
    {
        using var app = NewInstallation();

        Assert.False(Run(app, Settings(null, null)));
        Assert.False(Run(app, Settings("", "")));

        Assert.Equal(0, app.Query(db => db.AppUsers.Count()));
    }

    [Fact]
    public async Task Restarting_never_resets_an_existing_account()
    {
        using var app = NewInstallation();
        Run(app, Settings("owner", Password));

        // The settings are still there at the next start, with a different password.
        Assert.False(Run(app, Settings("owner", "another-strong-password")));
        Assert.False(Run(app, Settings("someone-else", Password)));

        Assert.Equal(HttpStatusCode.OK, (await SignIn(app, "owner", Password)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SignIn(app, "owner", "another-strong-password")).StatusCode);
        Assert.Equal(1, app.Query(db => db.AppUsers.Count()));
    }

    [Fact]
    public void When_other_accounts_exist_no_new_one_is_added()
    {
        using var app = new ApiFactory(); // already has the test secretary

        Assert.False(Run(app, Settings("owner", Password)));

        Assert.Equal(1, app.Query(db => db.AppUsers.Count()));
    }

    [Theory]
    [InlineData("owner", null)]
    [InlineData("owner", "")]
    [InlineData(null, Password)]
    [InlineData("   ", Password)]
    public void Settings_that_are_only_half_given_stop_the_start_with_a_clear_message(string? username, string? password)
    {
        using var app = NewInstallation();

        var error = Assert.Throws<InvalidOperationException>(() => Run(app, Settings(username, password)));

        Assert.Contains("together", error.Message);
        Assert.Equal(0, app.Query(db => db.AppUsers.Count()));
    }

    [Theory]
    [InlineData(11, false)] // one character short: refused
    [InlineData(12, true)]  // the shortest accepted
    [InlineData(1, false)]
    public void The_password_must_be_at_least_12_characters(int length, bool accepted)
    {
        using var app = NewInstallation();
        var password = new string('x', length);

        if (accepted)
        {
            Assert.True(Run(app, Settings("owner", password)));
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => Run(app, Settings("owner", password)));
            Assert.Equal(0, app.Query(db => db.AppUsers.Count()));
        }
    }

    [Fact]
    public void The_username_is_stored_in_lower_case_like_every_other_account()
    {
        using var app = NewInstallation();

        Run(app, Settings("  Owner  ", Password));

        Assert.Equal("owner", app.Query(db => db.AppUsers.Single().Username));
    }
}
