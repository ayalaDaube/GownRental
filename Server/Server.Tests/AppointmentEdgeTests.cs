using System.Net;
using System.Net.Http.Json;
using Bl.Models;
using Dal.Models;

namespace Server.Tests;

/// <summary>
/// Fitting appointments at their edges. Rules: a slot can be booked only while it is free and still ahead of us;
/// cancelling frees it for the next person; one person can hold several slots but not the same slot twice.
/// </summary>
public class AppointmentEdgeTests
{
    private static async Task<AppointmentFullDetails> FirstFree(HttpClient client) =>
        (await client.GetFromJsonAsync<List<AppointmentFullDetails>>("/api/Appointment/getAvailableAppointments"))!.First();

    private static object Booking(AppointmentFullDetails slot, string customerId = "C000001", int? people = 2) => new
    {
        appointmentDate = slot.AppointmentDate.ToString("yyyy-MM-dd"),
        hour = slot.Hour.ToString("HH:mm:ss"),
        customerId, numberOfPeople = people,
    };

    /// <summary>Adds a free slot at an exact moment and returns its id.</summary>
    private static int AddSlot(ApiFactory app, DateTime moment)
    {
        return app.Query(db =>
        {
            var slot = new Appointment
            {
                Date = DateOnly.FromDateTime(moment),
                Hour = new TimeOnly(moment.Hour, moment.Minute),
                IsAvailable = true,
            };
            db.Appointments.Add(slot);
            db.SaveChanges();
            return slot.Id;
        });
    }

    private static object BookingAt(DateTime moment, string customerId = "C000001") => new
    {
        appointmentDate = DateOnly.FromDateTime(moment).ToString("yyyy-MM-dd"),
        hour = new TimeOnly(moment.Hour, moment.Minute).ToString("HH:mm:ss"),
        customerId, numberOfPeople = 1,
    };

    // ---- what "still ahead of us" means

    [Fact]
    public async Task A_slot_a_few_minutes_ago_cannot_be_booked_and_one_a_few_minutes_ahead_can()
    {
        using var app = Api.SeededApp();
        var visitor = app.CreateClient();
        // Slots exist at whole minutes; use ones two minutes either side of now, away from the demo slots.
        var past = DateTime.Now.AddMinutes(-2);
        var ahead = DateTime.Now.AddMinutes(3);
        AddSlot(app, past);
        AddSlot(app, ahead);

        var tooLate = await visitor.PostAsJsonAsync("/api/Appointment", BookingAt(past));
        var inTime = await visitor.PostAsJsonAsync("/api/Appointment", BookingAt(ahead));

        Assert.Equal(HttpStatusCode.BadRequest, tooLate.StatusCode);
        Assert.Equal(HttpStatusCode.Created, inTime.StatusCode);
    }

    [Fact]
    public async Task The_list_of_free_slots_only_shows_slots_that_can_still_be_booked()
    {
        using var app = Api.SeededApp();
        var visitor = app.CreateClient();
        var pastId = AddSlot(app, DateTime.Now.AddMinutes(-2));
        var aheadId = AddSlot(app, DateTime.Now.AddMinutes(3));

        var free = (await visitor.GetFromJsonAsync<List<AppointmentFullDetails>>("/api/Appointment/getAvailableAppointments"))!;

        Assert.DoesNotContain(free, s => s.Id == pastId);
        Assert.Contains(free, s => s.Id == aheadId);
        var now = DateTime.Now;
        Assert.All(free, s => Assert.True(s.AppointmentDate.ToDateTime(s.Hour) > now.AddSeconds(-1)));
    }

    [Fact]
    public async Task The_free_slots_are_listed_from_the_earliest_to_the_latest()
    {
        using var app = Api.SeededApp();
        var visitor = app.CreateClient();
        AddSlot(app, DateTime.Today.AddDays(2).AddHours(7));   // earlier than the demo slots of that day

        var free = (await visitor.GetFromJsonAsync<List<AppointmentFullDetails>>("/api/Appointment/getAvailableAppointments"))!;
        var moments = free.Select(s => s.AppointmentDate.ToDateTime(s.Hour)).ToList();

        Assert.Equal(moments.OrderBy(m => m).ToList(), moments);
        Assert.Equal(moments.Distinct().Count(), moments.Count);
    }

    // ---- booking and cancelling

    [Fact]
    public async Task A_cancelled_slot_can_be_booked_by_someone_else_and_shows_in_the_free_list_again()
    {
        using var app = Api.SeededApp();
        var visitor = app.CreateClient();
        var staff = await app.SignedInClientAsync();
        var slot = await FirstFree(visitor);
        await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot, "C000001"));
        Assert.DoesNotContain((await visitor.GetFromJsonAsync<List<AppointmentFullDetails>>("/api/Appointment/getAvailableAppointments"))!, s => s.Id == slot.Id);

        await staff.DeleteAsync($"/api/Appointment/{slot.Id}");
        var listedAgain = (await visitor.GetFromJsonAsync<List<AppointmentFullDetails>>("/api/Appointment/getAvailableAppointments"))!;
        var rebooked = await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot, "C000002"));

        Assert.Contains(listedAgain, s => s.Id == slot.Id);
        Assert.Equal(HttpStatusCode.Created, rebooked.StatusCode);
        Assert.Equal("C000002", app.Query(db => db.Appointments.Find(slot.Id))!.CustomerId);
    }

    [Fact]
    public async Task Cancelling_the_same_booking_twice_is_a_conflict_the_second_time()
    {
        using var app = Api.SeededApp();
        var visitor = app.CreateClient();
        var staff = await app.SignedInClientAsync();
        var slot = await FirstFree(visitor);
        await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot));

        var first = await staff.DeleteAsync($"/api/Appointment/{slot.Id}");
        var second = await staff.DeleteAsync($"/api/Appointment/{slot.Id}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task The_same_person_booking_the_same_slot_twice_gets_a_conflict_and_keeps_one_booking()
    {
        using var app = Api.SeededApp();
        var visitor = app.CreateClient();
        var slot = await FirstFree(visitor);

        var first = await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot, "C000001", 2));
        var second = await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot, "C000001", 5));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal(2, app.Query(db => db.Appointments.Find(slot.Id))!.NumberOfPeople);
    }

    [Fact]
    public async Task One_person_may_hold_several_different_slots()
    {
        using var app = Api.SeededApp();
        var visitor = app.CreateClient();
        var slots = (await visitor.GetFromJsonAsync<List<AppointmentFullDetails>>("/api/Appointment/getAvailableAppointments"))!.Take(3).ToList();

        foreach (var slot in slots)
        {
            var response = await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot, "C000001"));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        Assert.Equal(3, app.Query(db => db.Appointments.Count(a => a.CustomerId == "C000001" && slots.Select(s => s.Id).Contains(a.Id))));
    }

    [Fact]
    public async Task The_customer_id_of_a_booking_is_matched_without_regard_to_case()
    {
        using var app = Api.SeededApp();
        var visitor = app.CreateClient();
        var slot = await FirstFree(visitor);
        var customers = app.Query(db => db.Customers.Count());

        var response = await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot, " c000004 "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("C000004", app.Query(db => db.Appointments.Find(slot.Id))!.CustomerId);
        Assert.Equal(customers, app.Query(db => db.Customers.Count()));
    }

    // ---- people

    [Theory]
    [InlineData(1, HttpStatusCode.Created)]
    [InlineData(10, HttpStatusCode.Created)]
    [InlineData(0, HttpStatusCode.BadRequest)]
    [InlineData(11, HttpStatusCode.BadRequest)]
    [InlineData(-1, HttpStatusCode.BadRequest)]
    [InlineData(int.MaxValue, HttpStatusCode.BadRequest)]
    public async Task The_number_of_people_is_between_one_and_ten(int people, HttpStatusCode expected)
    {
        using var app = Api.SeededApp();
        var visitor = app.CreateClient();
        var slot = await FirstFree(visitor);

        var response = await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot, "C000001", people));

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task When_the_number_of_people_is_left_out_it_is_one()
    {
        using var app = Api.SeededApp();
        var visitor = app.CreateClient();
        var slot = await FirstFree(visitor);

        var response = await visitor.PostAsJsonAsync("/api/Appointment", Booking(slot, "C000001", null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, app.Query(db => db.Appointments.Find(slot.Id))!.NumberOfPeople);
    }

    // ---- the staff view

    [Fact]
    public async Task The_list_of_a_day_has_only_that_day_in_time_order_with_booked_and_free_slots()
    {
        using var app = Api.SeededApp();
        var staff = await app.SignedInClientAsync();
        var day = (await staff.GetFromJsonAsync<List<AppointmentFullDetails>>("/api/Appointment"))!
            .Where(a => a.IsAvailable == false).Select(a => a.AppointmentDate).First();

        var list = (await staff.GetFromJsonAsync<List<AppointmentFullDetails>>($"/api/Appointment/getByDate/{day:yyyy-MM-dd}"))!;

        Assert.All(list, a => Assert.Equal(day, a.AppointmentDate));
        Assert.Equal(list.Select(a => a.Hour).OrderBy(h => h).ToList(), list.Select(a => a.Hour).ToList());
        Assert.Contains(list, a => a.IsAvailable == true);
        Assert.Contains(list, a => a.IsAvailable == false);
        Assert.All(list.Where(a => a.IsAvailable == false), a => Assert.False(string.IsNullOrEmpty(a.CustomerName)));
        Assert.All(list.Where(a => a.IsAvailable == true), a => Assert.True(string.IsNullOrEmpty(a.CustomerId)));
    }

    [Fact]
    public async Task A_day_without_slots_gives_an_empty_list_not_an_error()
    {
        using var app = Api.SeededApp();
        var staff = await app.SignedInClientAsync();

        var response = await staff.GetAsync("/api/Appointment/getByDate/2040-01-01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<List<AppointmentFullDetails>>())!);
    }

    [Fact]
    public async Task A_booked_appointment_shows_the_customer_the_staff_need_to_call()
    {
        using var app = Api.SeededApp();
        var visitor = app.CreateClient();
        var staff = await app.SignedInClientAsync();
        var slot = await FirstFree(visitor);
        await visitor.PostAsJsonAsync("/api/Appointment", new
        {
            appointmentDate = slot.AppointmentDate.ToString("yyyy-MM-dd"), hour = slot.Hour.ToString("HH:mm:ss"),
            customerId = "NEWBIE123", customerName = "דנה לוי", customerPhone = "050-123-4567", customerEmail = "dana@example.com", numberOfPeople = 3,
        });

        var seen = (await staff.GetFromJsonAsync<AppointmentFullDetails>($"/api/Appointment/{slot.Id}"))!;

        Assert.Equal("דנה לוי", seen.CustomerName);
        Assert.Equal("0501234567", seen.CustomerPhone);
        Assert.Equal("dana@example.com", seen.CustomerEmail);
        Assert.Equal(3, seen.NumberOfPeople);
        Assert.False(seen.IsAvailable);
    }
}
