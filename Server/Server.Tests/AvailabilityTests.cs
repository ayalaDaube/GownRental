using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bl.Models;

namespace Server.Tests;

public class AvailabilityTests
{
    // Demo data: gown 21 (model 1, size 44) has never been rented. Rentals in the demo
    // data are due between 45 days ago and 21 days ahead, so a date 40 days ahead is clear of them.

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);
    private static readonly DateOnly EventDay = Today.AddDays(40);

    private static ApiFactory NewApp()
    {
        var app = new ApiFactory();
        app.SeedDemoData();
        return app;
    }

    private static async Task<GownSizeInfo> SizeAsync(HttpClient client, string url, int gownId)
    {
        var models = (await client.GetFromJsonAsync<List<ModelGrouped>>(url))!;
        return models.SelectMany(m => m.Details).Single(d => d.GownId == gownId);
    }

    private static Task<HttpResponseMessage> RentAsync(HttpClient client, int gownId, int size, DateOnly eventDay) =>
        client.PostAsJsonAsync("/api/Rentals", new
        {
            customerId = "C000001",
            gownId,
            size,
            eventDate = eventDay.ToString("yyyy-MM-dd"),
        });

    [Fact]
    public async Task Without_a_date_every_gown_in_the_inventory_is_available()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var size = await SizeAsync(client, "/api/Gowns/groups", 21);

        Assert.True(size.IsActive);
        Assert.True(size.IsAvailable);
    }

    [Fact]
    public async Task Taking_a_gown_out_of_the_inventory_makes_it_unavailable_everywhere()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PutAsJsonAsync("/api/Gowns/21", new { size = 44, price = 90, isActive = false });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var noDate = await SizeAsync(client, "/api/Gowns/groups", 21);
        var withDate = await SizeAsync(client, $"/api/Gowns/byDate?date={EventDay:yyyy-MM-dd}", 21);
        Assert.False(noDate.IsActive);
        Assert.False(noDate.IsAvailable);
        Assert.False(withDate.IsAvailable);
    }

    [Fact]
    public async Task Renting_a_gown_does_not_retire_it_permanently()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var rental = await RentAsync(client, gownId: 21, size: 44, EventDay);

        Assert.Equal(HttpStatusCode.Created, rental.StatusCode);
        var size = await SizeAsync(client, "/api/Gowns/groups", 21);
        Assert.True(size.IsActive);
        Assert.True(size.IsAvailable);
        Assert.True(app.Query(db => db.ModelDetails.Find(21))!.IsActive);
    }

    [Theory]
    [InlineData(-4, true)]
    [InlineData(-3, false)]
    [InlineData(0, false)]
    [InlineData(3, false)]
    [InlineData(4, true)]
    public async Task A_rented_gown_is_unavailable_within_the_buffer_days_around_its_event(int daysFromEvent, bool expectedAvailable)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        await RentAsync(client, gownId: 21, size: 44, EventDay);

        var date = EventDay.AddDays(daysFromEvent);
        var size = await SizeAsync(client, $"/api/Gowns/byDate?date={date:yyyy-MM-dd}", 21);

        Assert.Equal(expectedAvailable, size.IsAvailable);
        Assert.True(size.IsActive);
    }

    [Fact]
    public async Task Other_gowns_stay_available_on_a_date_when_one_is_rented()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        await RentAsync(client, gownId: 21, size: 44, EventDay);

        var other = await SizeAsync(client, $"/api/Gowns/byDate?date={EventDay:yyyy-MM-dd}", 20);

        Assert.True(other.IsAvailable);
    }

    [Fact]
    public async Task Asking_for_a_date_in_the_past_is_a_400()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.GetAsync($"/api/Gowns/byDate?date={Today.AddDays(-2):yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());
        Assert.Contains("past", problem.GetProperty("detail").GetString());
    }
}
