using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bl.Models;

namespace Server.Tests;

public class CustomerTests
{
    // Demo data: customers C000001-C000015 (C000001 is "Alice Johnson"); gown 21 (size 44) has never been rented.

    private static readonly DateOnly EventDay = DateOnly.FromDateTime(DateTime.Today).AddDays(40);

    private static ApiFactory NewApp()
    {
        var app = new ApiFactory();
        app.SeedDemoData();
        return app;
    }

    private static object Rental(string customerId, string? name = null, string? phone = null, string? email = null) => new
    {
        customerId, gownId = 21, eventDate = EventDay.ToString("yyyy-MM-dd"),
        customerName = name, customerPhone = phone, customerEmail = email,
    };

    private static async Task<string?> DetailAsync(HttpResponseMessage response)
    {
        var problem = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());
        return problem.TryGetProperty("detail", out var detail) ? detail.GetString() : null;
    }

    [Fact]
    public async Task A_new_customer_is_registered_together_with_their_first_rental()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Rentals",
            Rental("a1b2c3d4", "Dana Levi", "050-123 4567", " dana@example.com "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var customer = app.Query(db => db.Customers.Find("A1B2C3D4"))!;
        Assert.Equal("Dana Levi", customer.Name);
        Assert.Equal("0501234567", customer.Phone);
        Assert.Equal("dana@example.com", customer.Email);
    }

    [Fact]
    public async Task A_new_customer_without_a_name_and_phone_is_refused_and_nothing_is_saved()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        var customers = app.Query(db => db.Customers.Count());
        var rentals = app.Query(db => db.Rentals.Count());

        var response = await client.PostAsJsonAsync("/api/Rentals", Rental("NOBODY1"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var detail = await DetailAsync(response);
        Assert.Contains("NOBODY1", detail);
        Assert.Contains("not registered", detail);
        Assert.Equal(customers, app.Query(db => db.Customers.Count()));
        Assert.Equal(rentals, app.Query(db => db.Rentals.Count()));
    }

    [Fact]
    public async Task A_rental_that_is_refused_does_not_leave_a_new_customer_behind()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();
        await client.PostAsJsonAsync("/api/Rentals", Rental("C000001"));
        var customers = app.Query(db => db.Customers.Count());

        // Same gown, same date: refused for the clash, so the would-be new customer must not appear.
        var response = await client.PostAsJsonAsync("/api/Rentals", Rental("NEWBIE12", "New Person", "0501234567"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(customers, app.Query(db => db.Customers.Count()));
        Assert.Null(app.Query(db => db.Customers.Find("NEWBIE12")));
    }

    [Theory]
    [InlineData("abc")]               // too short
    [InlineData("1234567890")]        // too long
    [InlineData("12 345")]            // contains a space
    [InlineData("AB-12345")]          // contains a dash
    public async Task An_id_in_the_wrong_format_is_rejected(string customerId)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Rentals", Rental(customerId, "Dana Levi", "0501234567"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("5 to 9", await DetailAsync(response));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("abcdefghi")]
    [InlineData("+97250123")]
    public async Task A_phone_in_the_wrong_format_is_rejected(string phone)
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Rentals", Rental("DANA1234", "Dana Levi", phone));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("phone", await DetailAsync(response));
        Assert.Null(app.Query(db => db.Customers.Find("DANA1234")));
    }

    [Fact]
    public async Task An_invalid_email_or_too_long_name_is_rejected()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var badEmail = await client.PostAsJsonAsync("/api/Rentals", Rental("DANA1234", "Dana Levi", "0501234567", "not-an-email"));
        var longName = await client.PostAsJsonAsync("/api/Rentals", Rental("DANA1234", new string('x', 61), "0501234567"));

        Assert.Equal(HttpStatusCode.BadRequest, badEmail.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, longName.StatusCode);
        Assert.Null(app.Query(db => db.Customers.Find("DANA1234")));
    }

    [Fact]
    public async Task The_id_is_matched_without_regard_to_case_and_spaces()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/Rentals", Rental("  c000001 "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("C000001", app.Query(db => db.Rentals.OrderBy(r => r.Id).Last().CustomerId));
    }

    [Fact]
    public async Task Details_sent_for_an_existing_customer_never_overwrite_theirs()
    {
        using var app = NewApp();
        var client = await app.SignedInClientAsync();

        await client.PostAsJsonAsync("/api/Rentals", Rental("C000001", "Mallory", "0509999999", "mallory@example.com"));

        var customer = app.Query(db => db.Customers.Find("C000001"))!;
        Assert.Equal("Alice Johnson", customer.Name);
        Assert.Equal("0501234567", customer.Phone);
    }

    [Fact]
    public async Task A_visitor_booking_an_appointment_cannot_overwrite_an_existing_customer()
    {
        using var app = NewApp();
        var visitor = app.CreateClient(); // not signed in
        var slots = (await visitor.GetFromJsonAsync<List<AppointmentFullDetaills>>("/api/Appointment/getAvailableAppointments"))!;
        var slot = slots.First();

        var response = await visitor.PostAsJsonAsync("/api/Appointment", new
        {
            appointmentDate = slot.AppointmentDate.ToString("yyyy-MM-dd"),
            hour = slot.Hour.ToString("HH:mm:ss"),
            customerId = "C000001",
            customerName = "Mallory",
            customerPhone = "0509999999",
            customerEmail = "mallory@example.com",
            numberOfPeople = 2,
        });

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var customer = app.Query(db => db.Customers.Find("C000001"))!;
        Assert.Equal("Alice Johnson", customer.Name);
        Assert.Equal("alice.johnson@example.com", customer.Email);
    }

    [Fact]
    public async Task A_visitor_booking_with_an_invalid_id_gets_a_clear_message()
    {
        using var app = NewApp();
        var visitor = app.CreateClient();
        var slot = (await visitor.GetFromJsonAsync<List<AppointmentFullDetaills>>("/api/Appointment/getAvailableAppointments"))!.First();

        var response = await visitor.PostAsJsonAsync("/api/Appointment", new
        {
            appointmentDate = slot.AppointmentDate.ToString("yyyy-MM-dd"),
            hour = slot.Hour.ToString("HH:mm:ss"),
            customerId = "1",
            customerName = "Dana",
            customerPhone = "0501234567",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("5 to 9", await DetailAsync(response));
    }
}
