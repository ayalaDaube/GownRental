using Bl.Api;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bl.Models;
using Dal.Models;
using Dal.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Server.Tests;

/// <summary>
/// Starts the real API in memory for tests, with an in-memory SQLite database instead of SQL Server
/// (it supports transactions and foreign keys, like the real one) and a known signing key.
/// One secretary account exists.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "unit-test-signing-key-unit-test-signing-key-0123456789";
    public const string SecretaryUsername = "test-secretary";
    public const string SecretaryPassword = "Test-Password-12345";

    static ApiFactory()
    {
        // Environment variables are read by the app before it is built, so they reach Program.Main reliably.
        Environment.SetEnvironmentVariable("Jwt__Key", SigningKey);
        Environment.SetEnvironmentVariable("RateLimit__LoginPerMinute", "1000");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Server=unused");
    }

    // An in-memory SQLite database lives as long as its connection stays open.
    private readonly SqliteConnection connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Any environment other than Development: no migrations, no demo data.
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var sqlServerOptions = services.Single(s => s.ServiceType == typeof(DbContextOptions<DataManager>));
            services.Remove(sqlServerOptions);
            connection.Open();
            services.AddDbContext<DataManager>(options => options.UseSqlite(connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<DataManager>().Database.EnsureCreated();
        scope.ServiceProvider.GetRequiredService<IBl>().BLAuth
            .EnsureUser(SecretaryUsername, SecretaryPassword, Roles.Secretary);
        return host;
    }

    /// <summary>Fills the database with the same fictional demo data the development server uses.</summary>
    public void SeedDemoData()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataManager>();
        DemoDataSeeder.Seed(db, DateOnly.FromDateTime(DateTime.Today));
    }

    /// <summary>Runs code against the database directly, to check what was really stored.</summary>
    public T Query<T>(Func<DataManager, T> query)
    {
        using var scope = Services.CreateScope();
        return query(scope.ServiceProvider.GetRequiredService<DataManager>());
    }

    /// <summary>A client that is already signed in as the test secretary.</summary>
    public async Task<HttpClient> SignedInClientAsync()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/Auth/login",
            new { username = SecretaryUsername, password = SecretaryPassword });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<Server.Controllers.LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.Token);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }
}
