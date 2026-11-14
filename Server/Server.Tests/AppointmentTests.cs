using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Bl.Models;
using Dal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Server.Tests;

public class AppointmentTests
{
    // Demo data: free fitting slots from today for three weeks (Friday and Saturday closed), a few of them
    // already booked, and customers C000001-C000015.

    private static ApiFactory NewApp()
    {
        var app = new ApiFactory();
        app.SeedDemoData();
        return app;
    }

    private static async Task<AppointmentFullDetaills> FirstFreeSlotAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<AppointmentFullDetaills>>("/api/Appointment/getAvailableAppointments"))!.First();

    private static object Booking(AppointmentFullDetaills slot, string customerId = "C000001", int people = 2,
        string? name = null, string? phone = null) => new
    {
        appointmentDate = slot.AppointmentDate.ToString("yyyy-MM-dd"),
        hour = slot.Hour.ToString("HH:mm:ss"),
        customerId, numberOfPeople = people, customerName = name, customerPhone = phone,
    };

    private static async Task<string?> DetailAsync(HttpResponseMessage response)
    {
        var problem = await JsonSerializer.DeserializeAsync<JsonElement>(await response.Content.ReadAsStreamAsync());
        return problem.TryGetProperty("detail", out var detail) ? detail.GetString() : null;
    }

    private static Appointment Slot(ApiFactory app, int id) => app.Query(db => db.Appointments.AsNoTracking().Single(a => a.Id == id));

    // ---- booking

    [Fact]
    public async Task A_visitor_can_book_a_free_slot_and_gets_a_confirmation_without_customer_details()
    {
        using var app = NewApp();
        var visitor = app.CreateClient();
        var slot = await FirstFreeSlotAsync(visitor);

        var response = await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot, "C000001", 3));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Alice", body);               // nothing stored about the customer is echoed back
        Assert.DoesNotContain("customerPhone", body, StringComparison.OrdinalIgnoreCase);
        var confirmation = JsonSerializer.Deserialize<BookingConfirmation>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(slot.Id, confirmation.Id);
        Assert.Equal(3, confirmation.NumberOfPeople);

        var stored = Slot(app, slot.Id);
        Assert.Equal("C000001", stored.CustomerId);
        Assert.False(stored.IsAvailable);
        Assert.Equal(3, stored.NumberOfPeople);
    }

    [Fact]
    public async Task A_new_customer_is_registered_when_booking()
    {
        using var app = NewApp();
        var visitor = app.CreateClient();
        var slot = await FirstFreeSlotAsync(visitor);

        var response = await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot, "newbie123", 1, "Dana Levi", "050-123-4567"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var customer = app.Query(db => db.Customers.Find("NEWBIE123"))!;
        Assert.Equal("Dana Levi", customer.Name);
        Assert.Equal("0501234567", customer.Phone);
    }

    [Fact]
    public async Task A_slot_that_is_already_booked_cannot_be_booked_again_and_the_first_booking_is_kept()
    {
        using var app = NewApp();
        var visitor = app.CreateClient();
        var slot = await FirstFreeSlotAsync(visitor);
        await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot, "C000001", 2));

        var second = await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot, "C000002", 5));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Contains("already booked", await DetailAsync(second));
        var stored = Slot(app, slot.Id);
        Assert.Equal("C000001", stored.CustomerId);
        Assert.Equal(2, stored.NumberOfPeople);
    }

    [Fact]
    public async Task A_booked_slot_disappears_from_the_free_list()
    {
        using var app = NewApp();
        var visitor = app.CreateClient();
        var slot = await FirstFreeSlotAsync(visitor);

        await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot));

        var free = (await visitor.GetFromJsonAsync<List<AppointmentFullDetaills>>("/api/Appointment/getAvailableAppointments"))!;
        Assert.DoesNotContain(free, s => s.Id == slot.Id);
    }

    [Fact]
    public async Task Booking_a_time_that_has_no_slot_is_a_404()
    {
        using var app = NewApp();
        var visitor = app.CreateClient();
        var slot = await FirstFreeSlotAsync(visitor);

        var response = await visitor.PostAsJsonAsync("/api/Appointment", new
        {
            appointmentDate = slot.AppointmentDate.ToString("yyyy-MM-dd"), hour = "03:17:00", customerId = "C000001",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_slot_in_the_past_cannot_be_booked_and_is_not_offered()
    {
        using var app = NewApp();
        var visitor = app.CreateClient();
        var pastId = app.Query(db =>
        {
            var past = new Appointment { Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), Hour = new TimeOnly(10, 0), IsAvailable = true };
            db.Appointments.Add(past);
            db.SaveChanges();
            return past.Id;
        });
        var free = (await visitor.GetFromJsonAsync<List<AppointmentFullDetaills>>("/api/Appointment/getAvailableAppointments"))!;

        var response = await visitor.PostAsJsonAsync("/api/Appointment", new
        {
            appointmentDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"), hour = "10:00:00", customerId = "C000001",
        });

        Assert.DoesNotContain(free, s => s.Id == pastId);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("passed", await DetailAsync(response));
        Assert.True(Slot(app, pastId).IsAvailable);
    }

    [Fact]
    public async Task The_free_list_holds_no_customer_data()
    {
        using var app = NewApp();
        var visitor = app.CreateClient();

        var json = await visitor.GetStringAsync("/api/Appointment/getAvailableAppointments");

        Assert.DoesNotContain("customerName\":\"", json);
        Assert.DoesNotContain("@example.com", json);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-3)]
    public async Task An_unreasonable_number_of_people_is_rejected(int people)
    {
        using var app = NewApp();
        var visitor = app.CreateClient();
        var slot = await FirstFreeSlotAsync(visitor);

        var response = await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot, "C000001", people));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(Slot(app, slot.Id).IsAvailable);
    }

    [Fact]
    public async Task A_booking_that_fails_on_the_customer_details_leaves_the_slot_free()
    {
        using var app = NewApp();
        var visitor = app.CreateClient();
        var slot = await FirstFreeSlotAsync(visitor);

        var response = await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot, "newbie123", 1, "Dana Levi", "123"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(Slot(app, slot.Id).IsAvailable);
        Assert.Null(app.Query(db => db.Customers.Find("NEWBIE123")));
    }

    [Fact]
    public void Two_people_booking_the_same_slot_at_the_same_moment_cannot_both_succeed()
    {
        using var app = NewApp();
        using var scopeA = app.Services.CreateScope();
        using var scopeB = app.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<DataManager>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<DataManager>();

        // Both read the same free slot before either one saves.
        var slotA = dbA.Appointments.First(a => a.IsAvailable == true);
        var slotB = dbB.Appointments.Single(a => a.Id == slotA.Id);
        Assert.True(slotB.IsAvailable);

        slotA.CustomerId = "C000001"; slotA.IsAvailable = false;
        dbA.SaveChanges();
        slotB.CustomerId = "C000002"; slotB.IsAvailable = false;

        Assert.Throws<DbUpdateConcurrencyException>(() => dbB.SaveChanges());
        Assert.Equal("C000001", Slot(app, slotA.Id).CustomerId);
    }

    // ---- staff: read and cancel

    [Fact]
    public async Task Staff_can_cancel_a_booking_and_the_slot_becomes_free_again()
    {
        using var app = NewApp();
        var visitor = app.CreateClient();
        var slot = await FirstFreeSlotAsync(visitor);
        await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot));
        var staff = await app.SignedInClientAsync();

        var response = await staff.DeleteAsync($"/api/Appointment/{slot.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = Slot(app, slot.Id);
        Assert.True(stored.IsAvailable);
        Assert.Null(stored.CustomerId);
        Assert.Null(stored.NumberOfPeople);
        Assert.Equal(slot.Hour, stored.Hour);
    }

    [Fact]
    public async Task Cancelling_an_unknown_appointment_is_a_404_and_a_slot_that_is_not_booked_is_a_409()
    {
        using var app = NewApp();
        var staff = await app.SignedInClientAsync();
        var free = await FirstFreeSlotAsync(staff);

        var unknown = await staff.DeleteAsync("/api/Appointment/99999");
        var notBooked = await staff.DeleteAsync($"/api/Appointment/{free.Id}");

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, notBooked.StatusCode);
    }

    [Fact]
    public async Task Only_staff_can_cancel_or_read_appointments_with_customers()
    {
        using var app = NewApp();
        var visitor = app.CreateClient();
        var slot = await FirstFreeSlotAsync(visitor);

        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.DeleteAsync($"/api/Appointment/{slot.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync("/api/Appointment")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync($"/api/Appointment/{slot.Id}")).StatusCode);
    }

    [Fact]
    public async Task Staff_can_read_a_single_appointment_including_a_free_one_and_get_404_for_an_unknown_one()
    {
        using var app = NewApp();
        var staff = await app.SignedInClientAsync();
        var free = await FirstFreeSlotAsync(staff);

        var freeSlot = await staff.GetAsync($"/api/Appointment/{free.Id}");
        var unknown = await staff.GetAsync("/api/Appointment/99999");

        Assert.Equal(HttpStatusCode.OK, freeSlot.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [Fact]
    public async Task The_staff_list_and_the_by_date_list_include_both_booked_and_free_slots_with_customer_names()
    {
        using var app = NewApp();
        var staff = await app.SignedInClientAsync();
        var all = (await staff.GetFromJsonAsync<List<AppointmentFullDetaills>>("/api/Appointment"))!;
        var booked = all.First(a => a.IsAvailable == false);

        var day = (await staff.GetFromJsonAsync<List<AppointmentFullDetaills>>(
            $"/api/Appointment/getByDate/{booked.AppointmentDate:yyyy-MM-dd}"))!;

        Assert.Contains(all, a => a.IsAvailable == true);
        Assert.False(string.IsNullOrEmpty(booked.CustomerName));
        Assert.Contains(day, a => a.Id == booked.Id && !string.IsNullOrEmpty(a.CustomerName));
        Assert.Contains(day, a => a.IsAvailable == true);
        Assert.All(day, a => Assert.Equal(booked.AppointmentDate, a.AppointmentDate));
    }
}
