using Bl;
using Dal.Models;
using Dal.Seed;
using Microsoft.EntityFrameworkCore;

namespace Server.Tests;

/// <summary>
/// Two things that must hold no matter what the code above them does: the demo data obeys every business rule
/// on any day of the week, and the database itself refuses data that breaks its structure.
/// </summary>
public class DataIntegrityTests
{
    private static readonly DateOnly Monday = new(2027, 3, 1);   // a Monday: the next 7 days cover every weekday

    public static IEnumerable<object[]> EveryWeekday() => Enumerable.Range(0, 7).Select(d => new object[] { Monday.AddDays(d) });

    private static ApiFactory AppSeededOn(DateOnly today)
    {
        var app = new ApiFactory();
        app.Query(db => { DemoDataSeeder.Seed(db, today); return 0; });
        return app;
    }

    // ---- the demo data obeys the rules

    [Theory]
    [MemberData(nameof(EveryWeekday))]
    public void The_demo_data_obeys_every_rule_whatever_the_day_it_is_created(DateOnly today)
    {
        using var app = AppSeededOn(today);

        app.Query(db =>
        {
            // gowns
            var models = db.GownModels.AsNoTracking().ToList();
            var sizes = db.ModelDetails.AsNoTracking().ToList();
            Assert.NotEmpty(models);
            Assert.All(models, m =>
            {
                Assert.InRange((m.Name ?? "").Length, 1, 60);
                Assert.InRange(m.Color.Length, 1, 30);
                Assert.True((m.Description ?? "").Length <= 200);
                var own = sizes.Where(s => s.ModelId == m.Id).ToList();
                Assert.NotEmpty(own);                                                  // no model without a size
                Assert.Equal(own.Count, own.Select(s => s.Size).Distinct().Count());   // no size twice in a model
            });
            Assert.All(sizes, s =>
            {
                Assert.InRange(s.Size, 1, 100);
                Assert.InRange(s.Price, 1, 100000);
                Assert.True(s.IsActive);
            });

            // customers
            Assert.All(db.Customers.AsNoTracking().ToList(), c =>
            {
                Assert.Equal(c.Id, CustomerRules.NormalizeId(c.Id));
                Assert.Equal(c.Name, CustomerRules.NormalizeName(c.Name));
                Assert.Equal(c.Phone, CustomerRules.NormalizePhone(c.Phone));
                Assert.Equal(c.Email, CustomerRules.NormalizeEmail(c.Email));
            });

            // rentals
            var rentals = db.Rentals.AsNoTracking().ToList();
            var lines = db.RentalDetails.AsNoTracking().ToList();
            Assert.NotEmpty(rentals);
            Assert.All(rentals, r =>
            {
                var line = Assert.Single(lines, l => l.RentalId == r.Id);              // one gown per rental
                var gown = sizes.Single(s => s.GownId == line.GownId);
                Assert.Equal(gown.Size, line.Size);                                    // the size recorded is the gown's
                Assert.NotNull(db.Customers.Find(r.CustomerId));
                Assert.True(r.RentalDate.Date <= r.EventDate.ToDateTime(TimeOnly.MinValue));
            });
            foreach (var group in lines.GroupBy(l => l.GownId))
            {
                var days = group.Select(l => rentals.Single(r => r.Id == l.RentalId).EventDate).OrderBy(d => d).ToList();
                for (var i = 1; i < days.Count; i++)
                {
                    Assert.True(days[i].DayNumber - days[i - 1].DayNumber > RentalRules.BufferDays,
                        $"gown {group.Key} is rented for events {days[i - 1]} and {days[i]}, closer than the rule allows");
                }
            }

            // pick-ups and returns on the day it was created
            Assert.Equal(2, rentals.Count(r => r.EventDate == today.AddDays(RentalRules.BufferDays)));
            Assert.Equal(2, rentals.Count(r => r.EventDate == today.AddDays(-RentalRules.BufferDays)));

            // appointments
            var slots = db.Appointments.AsNoTracking().ToList();
            Assert.Equal(slots.Count, slots.Select(s => (s.Date, s.Hour)).Distinct().Count());
            Assert.All(slots, s =>
            {
                Assert.NotEqual(DayOfWeek.Friday, s.Date.DayOfWeek);
                Assert.NotEqual(DayOfWeek.Saturday, s.Date.DayOfWeek);
                Assert.InRange(s.Date.DayNumber, today.DayNumber, today.AddDays(21).DayNumber);
                if (s.IsAvailable == true)
                {
                    Assert.Null(s.CustomerId);
                    Assert.Null(s.NumberOfPeople);
                }
                else
                {
                    Assert.NotNull(db.Customers.Find(s.CustomerId));
                    Assert.InRange(s.NumberOfPeople ?? 0, 1, 10);
                }
            });
            Assert.True(slots.Count(s => s.IsAvailable == true) > 50, "there should be plenty of free slots to try");
            return 0;
        });
    }

    [Fact]
    public void Running_the_seeder_again_changes_nothing()
    {
        using var app = AppSeededOn(Monday);
        int Count(Func<Dal.Models.DataManager, int> q) => app.Query(q);
        var before = new[]
        {
            Count(db => db.GownModels.Count()), Count(db => db.ModelDetails.Count()), Count(db => db.Customers.Count()),
            Count(db => db.Rentals.Count()), Count(db => db.RentalDetails.Count()), Count(db => db.Appointments.Count()),
        };

        app.Query(db => { DemoDataSeeder.Seed(db, Monday); return 0; });
        app.Query(db => { DemoDataSeeder.Seed(db, Monday); return 0; });

        var after = new[]
        {
            Count(db => db.GownModels.Count()), Count(db => db.ModelDetails.Count()), Count(db => db.Customers.Count()),
            Count(db => db.Rentals.Count()), Count(db => db.RentalDetails.Count()), Count(db => db.Appointments.Count()),
        };
        Assert.Equal(before, after);
    }

    [Fact]
    public void Starting_the_server_again_days_later_adds_new_free_slots_without_touching_bookings()
    {
        using var app = AppSeededOn(Monday);
        var bookedBefore = app.Query(db => db.Appointments.Where(a => a.IsAvailable == false)
            .Select(a => new { a.Id, a.CustomerId, a.Date, a.Hour }).ToList());

        app.Query(db => { DemoDataSeeder.Seed(db, Monday.AddDays(10)); return 0; });

        app.Query(db =>
        {
            var slots = db.Appointments.AsNoTracking().ToList();
            Assert.Equal(slots.Count, slots.Select(s => (s.Date, s.Hour)).Distinct().Count());
            // every open day in the new three weeks has all nine hours
            for (var d = Monday.AddDays(10); d <= Monday.AddDays(31); d = d.AddDays(1))
            {
                if (d.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday) continue;
                Assert.Equal(9, slots.Count(s => s.Date == d));
            }
            foreach (var booked in bookedBefore)
            {
                var now = db.Appointments.Find(booked.Id)!;
                Assert.Equal(booked.CustomerId, now.CustomerId);
                Assert.False(now.IsAvailable);
            }
            return 0;
        });
    }

    // ---- the database refuses broken data on its own

    [Fact]
    public void A_rental_line_cannot_point_to_a_gown_that_does_not_exist()
    {
        using var app = Api.SeededApp();

        Assert.Throws<DbUpdateException>(() => app.Query(db =>
        {
            db.RentalDetails.Add(new RentalDetails { RentalId = 1, GownId = 9999, Size = 36 });
            db.SaveChanges();
            return 0;
        }));
    }

    [Fact]
    public void A_rental_cannot_belong_to_a_customer_that_does_not_exist()
    {
        using var app = Api.SeededApp();

        Assert.Throws<DbUpdateException>(() => app.Query(db =>
        {
            db.Rentals.Add(new Rental { CustomerId = "GHOST1234", EventDate = Monday, RentalDate = DateTime.Now });
            db.SaveChanges();
            return 0;
        }));
    }

    [Fact]
    public void A_customer_who_has_rentals_cannot_be_deleted()
    {
        using var app = Api.SeededApp();

        Assert.Throws<DbUpdateException>(() => app.Query(db =>
        {
            db.Customers.Remove(db.Customers.Find("C000001")!);
            db.SaveChanges();
            return 0;
        }));
        Assert.NotNull(app.Query(db => db.Customers.Find("C000001")));
    }

    [Fact]
    public void A_gown_that_was_rented_cannot_be_deleted_straight_from_the_database_either()
    {
        using var app = Api.SeededApp();

        Assert.Throws<DbUpdateException>(() => app.Query(db =>
        {
            db.ModelDetails.Remove(db.ModelDetails.Find(1)!);
            db.SaveChanges();
            return 0;
        }));
    }

    [Fact]
    public void A_rental_cannot_be_deleted_while_it_still_has_its_gown_line()
    {
        using var app = Api.SeededApp();

        Assert.Throws<DbUpdateException>(() => app.Query(db =>
        {
            db.Rentals.Remove(db.Rentals.Find(1)!);
            db.SaveChanges();
            return 0;
        }));
    }

    [Fact]
    public void Two_appointments_cannot_share_a_date_and_an_hour()
    {
        using var app = Api.SeededApp();
        var existing = app.Query(db => db.Appointments.AsNoTracking().First());

        Assert.Throws<DbUpdateException>(() => app.Query(db =>
        {
            db.Appointments.Add(new Appointment { Date = existing.Date, Hour = existing.Hour, IsAvailable = true });
            db.SaveChanges();
            return 0;
        }));
    }

    [Fact]
    public void Two_accounts_cannot_share_a_username()
    {
        using var app = new ApiFactory();

        Assert.Throws<DbUpdateException>(() => app.Query(db =>
        {
            db.AppUsers.Add(new AppUser { Username = ApiFactory.SecretaryUsername, PasswordHash = "x", Role = "Secretary" });
            db.SaveChanges();
            return 0;
        }));
    }

    [Fact]
    public void An_appointment_cannot_belong_to_a_customer_that_does_not_exist()
    {
        using var app = Api.SeededApp();

        Assert.Throws<DbUpdateException>(() => app.Query(db =>
        {
            var slot = db.Appointments.First(a => a.IsAvailable == true);
            slot.CustomerId = "GHOST1234";
            slot.IsAvailable = false;
            db.SaveChanges();
            return 0;
        }));
    }
}
