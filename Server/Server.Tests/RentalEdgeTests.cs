using System.Net;
using System.Net.Http.Json;
using Bl.Models;
using Dal.Models;

namespace Server.Tests;

/// <summary>
/// Rentals and orders at the edges: the first and last allowed day, what deleting or moving frees up,
/// and what is left behind when something is refused.
/// Demo data: rentals 1-10; gowns 20 and 21 (size 44) and 22 and 23 (model 14) were never rented; customers C000001-C000015.
/// </summary>
public class RentalEdgeTests
{
    private static readonly DateOnly Today = Api.Today;

    private static object Put(int rentalId, int gownId, DateOnly day, string customerId = "C000001") =>
        new { rentalId, customerId, gownId, eventDate = day.ToString("yyyy-MM-dd") };

    // ---- the first and last allowed day

    [Fact]
    public async Task An_event_today_is_allowed_and_one_yesterday_is_not()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        var today = await Api.Rent(client, 21, Today);
        var yesterday = await Api.Rent(client, 20, Today.AddDays(-1));

        Assert.Equal(HttpStatusCode.Created, today.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, yesterday.StatusCode);
    }

    [Theory]
    [InlineData(2 * 365)]            // two years ahead is still planning ahead
    public async Task An_event_up_to_two_years_ahead_is_allowed(int days)
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        var response = await Api.Rent(client, 21, Today.AddDays(days));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(2 * 365 + 40)]
    [InlineData(10 * 365)]
    public async Task An_event_far_in_the_future_is_refused_with_a_message(int days)
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        var response = await Api.Rent(client, 21, Today.AddDays(days));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("ahead", await Api.DetailAsync(response));
    }

    [Theory]
    [InlineData("9999-12-31")]
    [InlineData("9999-12-29")]
    [InlineData("0001-01-01")]
    [InlineData("0001-01-02")]
    public async Task The_smallest_and_largest_possible_dates_are_refused_without_an_error(string date)
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        var rental = await client.PostAsJsonAsync("/api/Rentals", new { customerId = "C000001", gownId = 21, eventDate = date });
        var order = await client.PostAsJsonAsync("/api/Orders", new { customerId = "C000001", eventDate = date, gownIds = new[] { 21 } });
        var availability = await client.GetAsync($"/api/Gowns/byDate?date={date}");

        Assert.Equal(HttpStatusCode.BadRequest, rental.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, order.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, availability.StatusCode);
    }

    [Fact]
    public async Task Moving_a_rental_to_a_far_future_date_is_refused_and_the_rental_stays_put()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        var rental = await Api.RentOk(client, 21, Today.AddDays(40));

        var response = await client.PutAsJsonAsync("/api/Rentals", Put(rental.RentalId, 21, DateOnly.MaxValue));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(Today.AddDays(40), app.Query(db => db.Rentals.Find(rental.RentalId))!.EventDate);
    }

    // ---- deleting and moving free the gown

    [Fact]
    public async Task Deleting_a_rental_frees_the_gown_for_that_date()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        var day = Today.AddDays(40);
        var rental = await Api.RentOk(client, 21, day);
        Assert.Equal(HttpStatusCode.Conflict, (await Api.Rent(client, 21, day, "C000002")).StatusCode);

        await client.DeleteAsync($"/api/Rentals/{rental.RentalId}");

        var availability = (await Api.Groups(client, $"/api/Gowns/byDate?date={day:yyyy-MM-dd}")).SelectMany(m => m.Details).Single(d => d.GownId == 21);
        Assert.True(availability.IsAvailable);
        Assert.Equal(HttpStatusCode.Created, (await Api.Rent(client, 21, day, "C000002")).StatusCode);
    }

    [Fact]
    public async Task A_gown_whose_only_rental_was_deleted_can_be_deleted()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        var rental = await Api.RentOk(client, 21, Today.AddDays(40));
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync("/api/Gowns/21")).StatusCode);

        await client.DeleteAsync($"/api/Rentals/{rental.RentalId}");

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/Gowns/21")).StatusCode);
    }

    [Fact]
    public async Task Moving_a_rental_to_another_gown_frees_the_first_and_blocks_the_second()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        var day = Today.AddDays(40);
        var rental = await Api.RentOk(client, 21, day);

        var moved = await client.PutAsJsonAsync("/api/Rentals", Put(rental.RentalId, 20, day));

        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        var sizes = (await Api.Groups(client, $"/api/Gowns/byDate?date={day:yyyy-MM-dd}")).SelectMany(m => m.Details).ToDictionary(d => d.GownId);
        Assert.True(sizes[21].IsAvailable);
        Assert.False(sizes[20].IsAvailable);
        Assert.Equal(1, app.Query(db => db.RentalDetails.Count(d => d.RentalId == rental.RentalId)));
    }

    [Fact]
    public async Task A_refused_move_changes_nothing_at_all()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        var day = Today.AddDays(40);
        await Api.RentOk(client, 20, day, "C000002");
        var mine = await Api.RentOk(client, 21, day);

        // Gown 20 is taken that day: moving my rental onto it must fail and leave my rental on gown 21.
        var response = await client.PutAsJsonAsync("/api/Rentals", Put(mine.RentalId, 20, day, "C000003"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var stored = (await client.GetFromJsonAsync<RentalFullDetails>($"/api/Rentals/{mine.RentalId}"))!;
        Assert.Equal(21, stored.GownId);
        Assert.Equal("C000001", stored.CustomerId);
    }

    [Fact]
    public async Task A_rental_can_be_moved_to_today_but_not_to_yesterday()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        var rental = await Api.RentOk(client, 21, Today.AddDays(40));

        var toToday = await client.PutAsJsonAsync("/api/Rentals", Put(rental.RentalId, 21, Today));
        var toYesterday = await client.PutAsJsonAsync("/api/Rentals", Put(rental.RentalId, 21, Today.AddDays(-1)));

        Assert.Equal(HttpStatusCode.OK, toToday.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, toYesterday.StatusCode);
        Assert.Equal(Today, app.Query(db => db.Rentals.Find(rental.RentalId))!.EventDate);
    }

    [Fact]
    public async Task Changing_only_the_customer_of_a_rental_never_conflicts_with_itself_even_next_to_another_rental()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        var day = Today.AddDays(40);
        await Api.RentOk(client, 21, day.AddDays(4));            // exactly far enough away
        var rental = await Api.RentOk(client, 21, day);

        var response = await client.PutAsJsonAsync("/api/Rentals", Put(rental.RentalId, 21, day, "C000005"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("C000005", app.Query(db => db.Rentals.Find(rental.RentalId))!.CustomerId);
    }

    // ---- a gown taken out of stock after it was rented

    [Fact]
    public async Task A_rental_of_a_gown_that_was_retired_later_can_still_be_edited_but_the_gown_cannot_be_rented_again()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        var rental = await Api.RentOk(client, 21, Today.AddDays(40));
        await client.PutAsJsonAsync("/api/Gowns/21", new { size = 44, price = 90, isActive = false });

        var edit = await client.PutAsJsonAsync("/api/Rentals", Put(rental.RentalId, 21, Today.AddDays(40), "C000009"));
        var again = await Api.Rent(client, 21, Today.AddDays(100));

        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    // ---- lists

    [Fact]
    public async Task One_customer_can_rent_two_different_gowns_for_the_same_day_as_two_rentals()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        var a = await Api.RentOk(client, 21, Today.AddDays(40));
        var b = await Api.RentOk(client, 20, Today.AddDays(40));

        Assert.NotEqual(a.RentalId, b.RentalId);
    }

    [Fact]
    public async Task The_rental_list_is_in_order_and_shows_every_rental_exactly_once()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        await Api.RentOk(client, 21, Today.AddDays(40));
        await Api.RentOk(client, 20, Today.AddDays(41));

        var list = (await client.GetFromJsonAsync<List<RentalFullDetails>>("/api/Rentals"))!;

        Assert.Equal(app.Query(db => db.Rentals.Count()), list.Count);
        Assert.Equal(list.Select(r => r.RentalId).Distinct().Count(), list.Count);
        Assert.Equal(list.Select(r => r.RentalId).OrderBy(i => i).ToList(), list.Select(r => r.RentalId).ToList());
    }

    private static void AddOldRental(ApiFactory app, DateOnly eventDay, int gownId)
    {
        app.Query(db =>
        {
            var rental = new Rental { CustomerId = "C000001", EventDate = eventDay, RentalDate = DateTime.Now.AddDays(-30) };
            db.Rentals.Add(rental);
            db.SaveChanges();
            db.RentalDetails.Add(new RentalDetails { RentalId = rental.Id, GownId = gownId, Size = db.ModelDetails.Find(gownId)!.Size });
            db.SaveChanges();
            return 0;
        });
    }

    [Fact]
    public async Task The_pick_up_and_return_lists_hold_exactly_the_events_three_days_away_and_nothing_nearby()
    {
        using var app = new ApiFactory();
        app.Query(db => { Dal.Seed.DemoDataSeeder.Seed(db, Today); return 0; });
        // Remove the demo rentals so that only the ones made here can show up.
        app.Query(db => { db.RentalDetails.RemoveRange(db.RentalDetails); db.Rentals.RemoveRange(db.Rentals); db.SaveChanges(); return 0; });
        var client = await app.SignedInClientAsync();
        // Events: 4, 3, 2 days ahead and 2, 3, 4 days ago, each with a different gown.
        var offsets = new[] { 4, 3, 2, -2, -3, -4 };
        for (var i = 0; i < offsets.Length; i++)
        {
            if (offsets[i] < 0) AddOldRental(app, Today.AddDays(offsets[i]), 1 + i);
            else await Api.RentOk(client, 1 + i, Today.AddDays(offsets[i]));
        }

        var pickups = (await client.GetFromJsonAsync<List<RentalFullDetails>>("/api/Rentals/renting"))!;
        var returns = (await client.GetFromJsonAsync<List<RentalFullDetails>>("/api/Rentals/returning"))!;

        Assert.Equal(new[] { 2 }, pickups.Select(r => r.GownId).ToArray());   // the +3 rental used gown 2
        Assert.Equal(new[] { 5 }, returns.Select(r => r.GownId).ToArray());   // the -3 rental used gown 5
    }

    // ---- orders at their limits

    [Fact]
    public async Task An_order_of_exactly_twenty_gowns_is_accepted_and_twenty_one_is_refused()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        var day = Api.Future(200);
        var twenty = Enumerable.Range(1, 20).ToArray();

        var tooMany = await client.PostAsJsonAsync("/api/Orders", new { customerId = "C000001", eventDate = day, gownIds = Enumerable.Range(1, 21).ToArray() });
        var rentals = app.Query(db => db.Rentals.Count());
        var ok = await client.PostAsJsonAsync("/api/Orders", new { customerId = "C000001", eventDate = day, gownIds = twenty });

        Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        var result = (await ok.Content.ReadFromJsonAsync<OrderResult>())!;
        Assert.Equal(twenty, result.Rentals.Select(r => r.GownId).ToArray());
        Assert.Equal(rentals + 20, app.Query(db => db.Rentals.Count()));
    }

    [Fact]
    public async Task Sending_the_same_order_twice_keeps_the_first_and_refuses_the_second_completely()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        var order = new { customerId = "C000001", eventDate = Api.Future(), gownIds = new[] { 20, 21, 22 } };
        await client.PostAsJsonAsync("/api/Orders", order);
        var after = app.Query(db => db.Rentals.Count());

        var second = await client.PostAsJsonAsync("/api/Orders", order);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(after, app.Query(db => db.Rentals.Count()));
    }

    [Fact]
    public async Task A_conflict_on_the_first_gown_of_an_order_saves_nothing_for_the_others()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();
        await Api.RentOk(client, 21, Today.AddDays(40), "C000002");
        var rentals = app.Query(db => db.Rentals.Count());

        var response = await client.PostAsJsonAsync("/api/Orders", new { customerId = "C000001", eventDate = Api.Future(41), gownIds = new[] { 21, 20, 22 } });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(rentals, app.Query(db => db.Rentals.Count()));
    }

    [Fact]
    public async Task An_order_may_be_for_today_and_two_customers_may_order_different_gowns_for_the_same_day()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        var first = await client.PostAsJsonAsync("/api/Orders", new { customerId = "C000001", eventDate = Api.Future(0), gownIds = new[] { 20 } });
        var second = await client.PostAsJsonAsync("/api/Orders", new { customerId = "C000002", eventDate = Api.Future(0), gownIds = new[] { 21 } });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task What_an_order_returns_is_exactly_what_the_rental_list_then_shows()
    {
        using var app = Api.SeededApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Orders", new { customerId = "c000003", eventDate = Api.Future(), gownIds = new[] { 22, 23 } });
        var result = (await response.Content.ReadFromJsonAsync<OrderResult>())!;
        var list = (await client.GetFromJsonAsync<List<RentalFullDetails>>("/api/Rentals"))!;

        Assert.Equal("C000003", result.CustomerId);
        foreach (var rental in result.Rentals)
        {
            var listed = Assert.Single(list, r => r.RentalId == rental.RentalId);
            Assert.Equal("C000003", listed.CustomerId);
            Assert.Equal(rental.GownId, listed.GownId);
            Assert.Equal(rental.EventDate, listed.EventDate);
        }
    }
}
