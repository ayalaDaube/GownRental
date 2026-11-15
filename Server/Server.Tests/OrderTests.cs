using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bl.Models;

namespace Server.Tests;

public class OrderTests
{
    // Demo data: gowns 20 and 21 (size 44) and 22 and 23 (model 14) have never been rented;
    // customers C000001-C000015 exist.

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);
    private static readonly DateOnly EventDay = Today.AddDays(40);

    private static ApiFactory NewApp()
    {
        var app = new ApiFactory();
        app.SeedDemoData();
        return app;
    }

    private static object Order(string customerId, DateOnly eventDay, int[] gownIds, string? name = null, string? phone = null) => new
    {
        customerId, eventDate = eventDay.ToString("yyyy-MM-dd"), gownIds, customerName = name, customerPhone = phone,
    };

    private static async Task<string?> DetailAsync(HttpResponseMessage response)
    {
        var problem = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());
        return problem.TryGetProperty("detail", out var detail) ? detail.GetString() : null;
    }

    private static int Rentals(ApiFactory app) => app.Query(db => db.Rentals.Count());
    private static int Lines(ApiFactory app) => app.Query(db => db.RentalDetails.Count());
    private static int Customers(ApiFactory app) => app.Query(db => db.Customers.Count());

    [Fact]
    public async Task An_order_rents_every_gown_in_it_for_the_same_event()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var before = Rentals(app);

        var response = await client.PostAsJsonAsync("/api/Orders", Order("C000001", EventDay, new[] { 20, 21, 22 }));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<OrderResult>())!;
        Assert.Equal(3, result.Rentals.Count);
        Assert.All(result.Rentals, r => Assert.Equal(EventDay, r.EventDate));
        Assert.Equal(new[] { 20, 21, 22 }, result.Rentals.Select(r => r.GownId).ToArray());
        Assert.Equal(before + 3, Rentals(app));
        Assert.Equal("C000001", result.CustomerId);
    }

    [Fact]
    public async Task A_new_customer_is_registered_once_for_the_whole_order()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var customers = Customers(app);

        var response = await client.PostAsJsonAsync("/api/Orders",
            Order("newbie123", EventDay, new[] { 20, 21 }, "Dana Levi", "050-123-4567"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(customers + 1, Customers(app));
        Assert.Equal("Dana Levi", app.Query(db => db.Customers.Find("NEWBIE123"))!.Name);
    }

    [Fact]
    public async Task If_one_gown_cannot_be_rented_the_whole_order_is_refused_and_nothing_is_saved()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        // Gown 20 is taken for that day, so an order of gowns 21 and 20 must fail on its second gown.
        await client.PostAsJsonAsync("/api/Rentals", new { customerId = "C000002", gownId = 20, eventDate = EventDay.ToString("yyyy-MM-dd") });
        var rentals = Rentals(app);
        var lines = Lines(app);
        var customers = Customers(app);

        var response = await client.PostAsJsonAsync("/api/Orders",
            Order("newbie123", EventDay, new[] { 21, 20 }, "Dana Levi", "0501234567"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("Gown 20", await DetailAsync(response));
        Assert.Equal(rentals, Rentals(app));          // gown 21's rental, made first, was rolled back
        Assert.Equal(lines, Lines(app));
        Assert.Equal(customers, Customers(app));      // and so was the new customer
        Assert.Null(app.Query(db => db.Customers.Find("NEWBIE123")));
    }

    [Fact]
    public async Task A_gown_that_is_out_of_stock_stops_the_whole_order()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        await client.PutAsJsonAsync("/api/Gowns/22", new { size = 36, price = 230, isActive = false });
        var rentals = Rentals(app);

        var response = await client.PostAsJsonAsync("/api/Orders", Order("C000001", EventDay, new[] { 21, 22 }));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(rentals, Rentals(app));
    }

    [Fact]
    public async Task An_unknown_gown_stops_the_whole_order_with_a_404()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var rentals = Rentals(app);

        var response = await client.PostAsJsonAsync("/api/Orders", Order("C000001", EventDay, new[] { 21, 9999 }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("9999", await DetailAsync(response));
        Assert.Equal(rentals, Rentals(app));
    }

    [Fact]
    public async Task A_gown_listed_twice_is_refused_before_anything_is_saved()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var rentals = Rentals(app);

        var response = await client.PostAsJsonAsync("/api/Orders", Order("C000001", EventDay, new[] { 21, 21 }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("more than once", await DetailAsync(response));
        Assert.Equal(rentals, Rentals(app));
    }

    [Fact]
    public async Task An_empty_order_or_one_with_too_many_gowns_is_a_400()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var empty = await client.PostAsJsonAsync("/api/Orders", Order("C000001", EventDay, Array.Empty<int>()));
        var tooMany = await client.PostAsJsonAsync("/api/Orders", Order("C000001", EventDay, Enumerable.Range(1, 21).ToArray()));

        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);
    }

    [Fact]
    public async Task An_event_in_the_past_is_refused_for_the_whole_order()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var rentals = Rentals(app);

        var response = await client.PostAsJsonAsync("/api/Orders", Order("C000001", Today.AddDays(-1), new[] { 21 }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(rentals, Rentals(app));
    }

    [Fact]
    public async Task An_unregistered_customer_without_details_is_refused()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var rentals = Rentals(app);

        var response = await client.PostAsJsonAsync("/api/Orders", Order("NOBODY1", EventDay, new[] { 21 }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("not registered", await DetailAsync(response));
        Assert.Equal(rentals, Rentals(app));
    }

    [Fact]
    public async Task Ordered_gowns_are_unavailable_on_that_date_but_not_forever()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        await client.PostAsJsonAsync("/api/Orders", Order("C000001", EventDay, new[] { 21, 22 }));

        var onTheDate = (await client.GetFromJsonAsync<List<ModelGrouped>>($"/api/Gowns/byDate?date={EventDay:yyyy-MM-dd}"))!
            .SelectMany(m => m.Details).ToDictionary(d => d.GownId);
        var noDate = (await client.GetFromJsonAsync<List<ModelGrouped>>("/api/Gowns/groups"))!
            .SelectMany(m => m.Details).ToDictionary(d => d.GownId);

        Assert.False(onTheDate[21].IsAvailable);
        Assert.False(onTheDate[22].IsAvailable);
        Assert.True(onTheDate[23].IsAvailable);
        Assert.True(noDate[21].IsAvailable);
    }

    [Fact]
    public async Task Placing_an_order_requires_signing_in()
    {
        using var app = NewApp();

        var response = await app.CreateClient().PostAsJsonAsync("/api/Orders", Order("C000001", EventDay, new[] { 21 }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
