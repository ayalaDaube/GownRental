using System.Net;
using System.Net.Http.Json;
using Bl.Api;
using Dal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Server.Tests;

/// <summary>How passwords are kept: never readable, upgraded when the storage method improves, and a damaged record never crashes the sign-in.</summary>
public class PasswordStorageTests
{
    private const string LegacyPassword = "Legacy-pass-123";

    private static string HashWithFewIterations(string password)
    {
        var weaker = new PasswordHasher<AppUser>(Options.Create(new PasswordHasherOptions { IterationCount = 10_000 }));
        return weaker.HashPassword(new AppUser { Username = "legacy" }, password);
    }

    private static void AddUser(ApiFactory app, string username, string hash) =>
        app.Query(db =>
        {
            db.AppUsers.Add(new AppUser { Username = username, Role = "Secretary", PasswordHash = hash });
            db.SaveChanges();
            return 0;
        });

    [Fact]
    public void A_password_saved_with_an_older_weaker_setting_still_works_and_is_saved_again_with_the_current_one()
    {
        using var app = new ApiFactory();
        var oldHash = HashWithFewIterations(LegacyPassword);
        AddUser(app, "legacy", oldHash);
        using var scope = app.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IBl>().BLAuth;

        var firstSignIn = auth.ValidateCredentials("legacy", LegacyPassword);
        var storedAfter = app.Query(db => db.AppUsers.Single(u => u.Username == "legacy").PasswordHash);
        var secondSignIn = auth.ValidateCredentials("legacy", LegacyPassword);
        var wrong = auth.ValidateCredentials("legacy", "Not-the-password-1");

        Assert.NotNull(firstSignIn);
        Assert.NotEqual(oldHash, storedAfter);        // upgraded
        Assert.NotNull(secondSignIn);                 // and still works afterwards
        Assert.Null(wrong);
    }

    [Theory]
    [InlineData("not a hash at all")]
    [InlineData("")]
    [InlineData("AQAAAAEAACcQAAAAEA==")]            // looks like a hash but is cut short
    public async Task A_damaged_password_record_means_the_sign_in_fails_cleanly_and_never_crashes(string brokenHash)
    {
        using var app = new ApiFactory();
        AddUser(app, "broken", brokenHash);

        var response = await app.CreateClient().PostAsJsonAsync("/api/Auth/login", new { username = "broken", password = "Anything-123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void The_same_password_is_stored_differently_for_every_account()
    {
        using var app = new ApiFactory();
        using var scope = app.Services.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IBl>().BLAuth;

        auth.EnsureUser("first", "Shared-password-1", "Secretary");
        auth.EnsureUser("second", "Shared-password-1", "Secretary");

        var hashes = app.Query(db => db.AppUsers.Where(u => u.Username == "first" || u.Username == "second").Select(u => u.PasswordHash).ToList());
        Assert.Equal(2, hashes.Distinct().Count());
        Assert.All(hashes, h => Assert.DoesNotContain("Shared-password-1", h));
    }
}
