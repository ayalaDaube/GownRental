using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bl.Models;

namespace Server.Tests;

/// <summary>Small helpers shared by the rule tests, so each test reads like the rule it checks.</summary>
internal static class Api
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    public static string Future(int days = 40) => Today.AddDays(days).ToString("yyyy-MM-dd");

    public static ApiFactory SeededApp()
    {
        var app = new ApiFactory();
        app.SeedDemoData();
        return app;
    }

    public static async Task<string?> DetailAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        try
        {
            var body = JsonSerializer.Deserialize<JsonElement>(text);
            return body.TryGetProperty("detail", out var detail) ? detail.GetString() : text;
        }
        catch (JsonException)
        {
            return text;
        }
    }

    public static async Task<RentalFullDetails> RentOk(HttpClient client, int gownId, DateOnly eventDay, string customerId = "C000001")
    {
        var response = await client.PostAsJsonAsync("/api/Rentals",
            new { customerId, gownId, eventDate = eventDay.ToString("yyyy-MM-dd") });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<RentalFullDetails>())!;
    }

    public static Task<HttpResponseMessage> Rent(HttpClient client, int gownId, DateOnly eventDay, string customerId = "C000001") =>
        client.PostAsJsonAsync("/api/Rentals", new { customerId, gownId, eventDate = eventDay.ToString("yyyy-MM-dd") });

    public static async Task<List<ModelGrouped>> Groups(HttpClient client, string url = "/api/Gowns/groups") =>
        (await client.GetFromJsonAsync<List<ModelGrouped>>(url))!;
}
