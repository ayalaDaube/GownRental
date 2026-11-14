using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bl.Models;

namespace Server.Tests;

public class RentalTests
{
    // Demo data: rentals 1-10 (rental 8 is a past event, 20 days ago); customers C000001-C000015;
    // gown 21 (size 44) and gown 20 (size 44) have never been rented.

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);
    private static readonly DateOnly EventDay = Today.AddDays(40);

    private static ApiFactory NewApp()
    {
        var app = new ApiFactory();
        app.SeedDemoData();
        return app;
    }

    private static object Body(int gownId, DateOnly eventDay, string customerId = "C000001", int size = 0) =>
        new { customerId, gownId, size, eventDate = eventDay.ToString("yyyy-MM-dd") };

    private static async Task<string?> DetailAsync(HttpResponseMessage response)
    {
        var problem = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());
        return problem.TryGetProperty("detail", out var detail) ? detail.GetString() : null;
    }

    private static int RentalCount(ApiFactory app) => app.Query(db => db.Rentals.Count());
    private static int DetailsCount(ApiFactory app) => app.Query(db => db.RentalDetails.Count());

    // ---- create

    [Fact]
    public async Task Renting_a_gown_stores_it_and_takes_the_size_from_the_gown()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Rentals", Body(21, EventDay));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<RentalFullDetaills>())!;
        Assert.Equal(11, created.rentalId);
        Assert.Equal(44, created.size);
        Assert.Equal(21, created.gownId);
        Assert.Equal(EventDay, created.eventDate);
        Assert.True((DateTime.Now - created.rentalDate).TotalMinutes < 5, "the booking time is set by the server");
        Assert.EndsWith("/api/Rentals/11", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task The_booking_time_cannot_be_chosen_by_the_caller()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Rentals", new
        {
            customerId = "C000001", gownId = 21, eventDate = EventDay.ToString("yyyy-MM-dd"),
            rentalDate = "2001-01-01T00:00:00",
        });

        var created = (await response.Content.ReadFromJsonAsync<RentalFullDetaills>())!;
        Assert.True(created.rentalDate.Year >= DateTime.Now.Year);
    }

    [Fact]
    public async Task A_size_that_contradicts_the_gown_is_rejected_and_the_matching_size_is_accepted()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var wrong = await client.PostAsJsonAsync("/api/Rentals", Body(21, EventDay, size: 36));
        var right = await client.PostAsJsonAsync("/api/Rentals", Body(21, EventDay, size: 44));

        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Contains("size 44", await DetailAsync(wrong));
        Assert.Equal(HttpStatusCode.Created, right.StatusCode);
    }

    [Fact]
    public async Task An_event_in_the_past_is_rejected()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Rentals", Body(21, Today.AddDays(-1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("past", await DetailAsync(response));
    }

    [Fact]
    public async Task An_unknown_gown_is_a_404_and_an_unregistered_customer_is_a_400_with_their_id_in_the_message()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var noGown = await client.PostAsJsonAsync("/api/Rentals", Body(9999, EventDay));
        var noCustomer = await client.PostAsJsonAsync("/api/Rentals", Body(21, EventDay, customerId: "NOBODY"));

        Assert.Equal(HttpStatusCode.NotFound, noGown.StatusCode);
        Assert.Contains("9999", await DetailAsync(noGown));
        Assert.Equal(HttpStatusCode.BadRequest, noCustomer.StatusCode);
        Assert.Contains("NOBODY", await DetailAsync(noCustomer));
    }

    [Fact]
    public async Task A_missing_customer_id_is_a_400()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Rentals", Body(21, EventDay, customerId: "  "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_gown_that_is_out_of_stock_cannot_be_rented()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        await client.PutAsJsonAsync("/api/Gowns/21", new { size = 44, price = 90, isActive = false });

        var response = await client.PostAsJsonAsync("/api/Rentals", Body(21, EventDay));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("out of stock", await DetailAsync(response));
    }

    [Theory]
    [InlineData(-4, true)]
    [InlineData(-3, false)]
    [InlineData(0, false)]
    [InlineData(3, false)]
    [InlineData(4, true)]
    public async Task The_same_gown_cannot_be_rented_for_events_closer_than_the_buffer(int daysApart, bool allowed)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        await client.PostAsJsonAsync("/api/Rentals", Body(21, EventDay));

        var second = await client.PostAsJsonAsync("/api/Rentals", Body(21, EventDay.AddDays(daysApart), customerId: "C000002"));

        Assert.Equal(allowed ? HttpStatusCode.Created : HttpStatusCode.Conflict, second.StatusCode);
        if (!allowed) Assert.Contains(EventDay.ToString("yyyy-MM-dd"), await DetailAsync(second));
    }

    [Fact]
    public async Task Two_different_gowns_can_be_rented_for_the_same_day()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        await client.PostAsJsonAsync("/api/Rentals", Body(21, EventDay));

        var other = await client.PostAsJsonAsync("/api/Rentals", Body(20, EventDay, customerId: "C000002"));

        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
    }

    [Fact]
    public async Task A_rejected_rental_leaves_nothing_behind()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var rentals = RentalCount(app);
        var details = DetailsCount(app);

        await client.PostAsJsonAsync("/api/Rentals", Body(21, EventDay, customerId: "NOBODY"));
        await client.PostAsJsonAsync("/api/Rentals", Body(21, Today.AddDays(-1)));
        await client.PostAsJsonAsync("/api/Rentals", Body(9999, EventDay));

        Assert.Equal(rentals, RentalCount(app));
        Assert.Equal(details, DetailsCount(app));
    }

    // ---- update

    [Fact]
    public async Task A_rental_can_be_moved_to_another_date_and_keeps_its_booking_time()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var created = (await (await client.PostAsJsonAsync("/api/Rentals", Body(21, EventDay))).Content.ReadFromJsonAsync<RentalFullDetaills>())!;

        var response = await client.PutAsJsonAsync("/api/Rentals", new
        {
            created.rentalId, customerId = "C000001", gownId = 21,
            eventDate = EventDay.AddDays(5).ToString("yyyy-MM-dd"),
            rentalDate = "2001-01-01T00:00:00",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<RentalFullDetaills>())!;
        Assert.Equal(EventDay.AddDays(5), updated.eventDate);
        Assert.Equal(created.rentalDate.Date, updated.rentalDate.Date);
    }

    [Fact]
    public async Task A_rental_does_not_conflict_with_itself_when_edited()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var created = (await (await client.PostAsJsonAsync("/api/Rentals", Body(21, EventDay))).Content.ReadFromJsonAsync<RentalFullDetaills>())!;

        var response = await client.PutAsJsonAsync("/api/Rentals", new
        {
            created.rentalId, customerId = "C000009", gownId = 21, eventDate = EventDay.ToString("yyyy-MM-dd"),
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Moving_a_rental_onto_a_date_that_clashes_with_another_is_a_conflict()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        await client.PostAsJsonAsync("/api/Rentals", Body(21, EventDay));
        var second = (await (await client.PostAsJsonAsync("/api/Rentals", Body(21, EventDay.AddDays(20), customerId: "C000002"))).Content.ReadFromJsonAsync<RentalFullDetaills>())!;

        var response = await client.PutAsJsonAsync("/api/Rentals", new
        {
            second.rentalId, customerId = "C000002", gownId = 21, eventDate = EventDay.AddDays(2).ToString("yyyy-MM-dd"),
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(EventDay.AddDays(20), app.Query(db => db.Rentals.Find(second.rentalId))!.EventDate);
    }

    [Fact]
    public async Task A_past_rental_can_still_be_edited_without_changing_its_date()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var past = (await client.GetFromJsonAsync<RentalFullDetaills>("/api/Rentals/8"))!;

        var response = await client.PutAsJsonAsync("/api/Rentals", new
        {
            past.rentalId, customerId = "C000009", gownId = past.gownId, eventDate = past.eventDate.ToString("yyyy-MM-dd"),
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("C000009", app.Query(db => db.Rentals.Find(8))!.CustomerId);
    }

    [Fact]
    public async Task Moving_a_rental_to_a_past_date_is_rejected()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var created = (await (await client.PostAsJsonAsync("/api/Rentals", Body(21, EventDay))).Content.ReadFromJsonAsync<RentalFullDetaills>())!;

        var response = await client.PutAsJsonAsync("/api/Rentals", new
        {
            created.rentalId, customerId = "C000001", gownId = 21, eventDate = Today.AddDays(-5).ToString("yyyy-MM-dd"),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Updating_an_unknown_rental_or_to_an_unknown_gown_is_a_404()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var created = (await (await client.PostAsJsonAsync("/api/Rentals", Body(21, EventDay))).Content.ReadFromJsonAsync<RentalFullDetaills>())!;

        var unknownRental = await client.PutAsJsonAsync("/api/Rentals", new { rentalId = 9999, customerId = "C000001", gownId = 21, eventDate = EventDay.ToString("yyyy-MM-dd") });
        var unknownGown = await client.PutAsJsonAsync("/api/Rentals", new { created.rentalId, customerId = "C000001", gownId = 9999, eventDate = EventDay.ToString("yyyy-MM-dd") });

        Assert.Equal(HttpStatusCode.NotFound, unknownRental.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownGown.StatusCode);
    }

    // ---- read and delete

    [Fact]
    public async Task Reading_an_unknown_rental_is_a_404()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.GetAsync("/api/Rentals/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_rental_removes_it_and_its_gown_line()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var rentals = RentalCount(app);
        var details = DetailsCount(app);

        var response = await client.DeleteAsync("/api/Rentals/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(rentals - 1, RentalCount(app));
        Assert.Equal(details - 1, DetailsCount(app));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/Rentals/1")).StatusCode);
    }

    [Fact]
    public async Task Deleting_an_unknown_rental_is_a_404()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.DeleteAsync("/api/Rentals/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_pickup_and_return_lists_match_the_events_three_days_ahead_and_ago()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var renting = (await client.GetFromJsonAsync<List<RentalFullDetaills>>("/api/Rentals/renting"))!;
        var returning = (await client.GetFromJsonAsync<List<RentalFullDetaills>>("/api/Rentals/returning"))!;

        Assert.Equal(2, renting.Count);
        Assert.All(renting, r => Assert.Equal(Today.AddDays(3), r.eventDate));
        Assert.Equal(2, returning.Count);
        Assert.All(returning, r => Assert.Equal(Today.AddDays(-3), r.eventDate));
    }
}
