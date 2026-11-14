using Dal.Models;

namespace Dal.Seed;

/// <summary>
/// Fills an empty database with fictional demo data so the application can be explored right after setup.
/// Dates are relative to <c>today</c>, so rentals due for pick-up or return and free fitting
/// appointments are always available. Safe to run on every start: it only adds what is missing.
/// </summary>
public static class DemoDataSeeder
{
    private const int AppointmentDaysAhead = 21;
    private static readonly TimeOnly[] AppointmentHours =
        Enumerable.Range(9, 9).Select(h => new TimeOnly(h, 0)).ToArray(); // 09:00 .. 17:00

    public static void Seed(DataManager db, DateOnly today)
    {
        SeedCustomers(db);
        SeedGowns(db);
        SeedRentals(db, today);
        SeedAppointments(db, today);
    }

    private static void SeedCustomers(DataManager db)
    {
        if (db.Customers.Any()) return;

        db.Customers.AddRange(
            Customer("C000001", "Alice Johnson", "0501234567", "alice.johnson@example.com"),
            Customer("C000002", "Bob Smith", "0502345678", "bob.smith@example.com"),
            Customer("C000003", "Catherine Brown", "0503456789", "catherine.brown@example.com"),
            Customer("C000004", "David Wilson", "0504567890", "david.wilson@example.com"),
            Customer("C000005", "Eva Green", "0505678901", "eva.green@example.com"),
            Customer("C000006", "Frank White", "0506789012", "frank.white@example.com"),
            Customer("C000007", "Grace Lee", "0507890123", "grace.lee@example.com"),
            Customer("C000008", "Henry Clark", "0508901234", "henry.clark@example.com"),
            Customer("C000009", "Isabella Lewis", "0509012345", "isabella.lewis@example.com"),
            Customer("C000010", "Jack Thompson", "0500123456", "jack.thompson@example.com"),
            Customer("C000011", "Karen Hall", "0501234568", "karen.hall@example.com"),
            Customer("C000012", "Larry Young", "0502345679", "larry.young@example.com"),
            Customer("C000013", "Mona King", "0503456780", "mona.king@example.com"),
            Customer("C000014", "Nina Scott", "0504567891", "nina.scott@example.com"),
            Customer("C000015", "Oscar Martinez", "0505678902", "oscar.martinez@example.com"));
        db.SaveChanges();
    }

    private static void SeedGowns(DataManager db)
    {
        if (db.GownModels.Any()) return;

        db.GownModels.AddRange(
            Model(1, "Simple Gown", "light blue", "A simple light blue gown"),
            Model(2, "Evening Gown", "Blue", "A light blue gown perfect for summer events."),
            Model(3, "Party Gown", "Black", "A stylish black gown for evening parties."),
            Model(4, "Wedding Gown", "White", "A beautiful white gown for weddings."),
            Model(5, "Cocktail Gown", "Pink", "A trendy pink cocktail gown."),
            Model(6, "Vintage Gown", "Green", "A vintage green gown with lace details."),
            Model(7, "Summer Gown", "Yellow", "A bright yellow gown for summer festivities."),
            Model(8, "Prom Gown", "Purple", "A stunning purple gown for prom nights."),
            Model(9, "Casual Gown", "Orange", "A casual orange gown for everyday wear."),
            Model(10, "Formal Gown", "Navy", "A navy formal gown for elegant events."),
            Model(11, "Floral Gown", "Floral", "A floral gown for spring celebrations."),
            Model(12, "Maxi Gown", "Teal", "A long teal maxi gown for relaxed outings."),
            Model(13, "Sheath Gown", "Burgundy", "A fitted burgundy sheath gown."),
            Model(14, "Tulle Gown", "Lavender", "A whimsical lavender tulle gown."),
            Model(15, "Wrap Gown", "Cyan", "A versatile cyan wrap gown."));

        db.ModelDetails.AddRange(
            Details(1, 1, 36, 220), Details(2, 1, 38, 250), Details(21, 1, 44, 90),
            Details(3, 2, 36, 200), Details(4, 2, 38, 200),
            Details(5, 3, 36, 180), Details(6, 3, 40, 180),
            Details(7, 4, 36, 500), Details(8, 4, 38, 500),
            Details(9, 5, 36, 150), Details(10, 5, 38, 150),
            Details(11, 6, 36, 220), Details(12, 6, 38, 220),
            Details(13, 7, 40, 120),
            Details(14, 8, 42, 300),
            Details(15, 9, 44, 90),
            Details(16, 10, 36, 210),
            Details(17, 11, 38, 130),
            Details(18, 12, 40, 160),
            Details(19, 13, 42, 140),
            Details(22, 14, 36, 230), Details(23, 14, 38, 230),
            Details(20, 15, 44, 110));
        db.SaveChanges();
    }

    private static void SeedRentals(DataManager db, DateOnly today)
    {
        if (db.Rentals.Any()) return;

        var sizeByGown = db.ModelDetails.ToDictionary(d => d.GownId, d => d.Size);

        // (customer, gown, event date relative to today). Events 3 days ahead appear in the
        // pick-up list and events 3 days ago in the return list.
        var plan = new (string Customer, int Gown, int EventInDays)[]
        {
            ("C000001", 1, 3),
            ("C000002", 7, 3),
            ("C000003", 3, -3),
            ("C000004", 11, -3),
            ("C000005", 8, 10),
            ("C000006", 14, 14),
            ("C000007", 16, 21),
            ("C000008", 2, -20),
            ("C000009", 12, -30),
            ("C000010", 17, -45),
        };

        var rentals = plan.Select(p =>
        {
            var eventDate = today.AddDays(p.EventInDays);
            return new Rental
            {
                CustomerId = p.Customer,
                EventDate = eventDate,
                RentalDate = eventDate.AddDays(-7).ToDateTime(new TimeOnly(10, 0)),
            };
        }).ToList();

        db.Rentals.AddRange(rentals);
        db.SaveChanges(); // generates the rental ids used by the details below

        db.RentalDetails.AddRange(rentals.Select((r, i) => new RentalDetails
        {
            RentalId = r.Id,
            GownId = plan[i].Gown,
            Size = sizeByGown[plan[i].Gown],
        }));
        db.SaveChanges();
    }

    private static void SeedAppointments(DataManager db, DateOnly today)
    {
        var last = today.AddDays(AppointmentDaysAhead);
        var existing = db.Appointments
            .Where(a => a.Date >= today && a.Date <= last)
            .Select(a => new { a.Date, a.Hour })
            .ToHashSet();

        // A few slots are already booked so the secretary's view has content too.
        var booked = new Dictionary<(int Day, int Hour), (string Customer, int People)>
        {
            [(1, 10)] = ("C000011", 2),
            [(1, 14)] = ("C000012", 3),
            [(2, 11)] = ("C000013", 1),
            [(3, 16)] = ("C000014", 4),
        };

        var slots = new List<Appointment>();
        for (var offset = 0; offset <= AppointmentDaysAhead; offset++)
        {
            var date = today.AddDays(offset);
            if (date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday) continue; // closed

            foreach (var hour in AppointmentHours)
            {
                if (existing.Contains(new { Date = date, Hour = hour })) continue;

                var slot = new Appointment { Date = date, Hour = hour, IsAvailable = true };
                if (booked.TryGetValue((offset, hour.Hour), out var booking))
                {
                    slot.CustomerId = booking.Customer;
                    slot.NumberOfPeople = booking.People;
                    slot.IsAvailable = false;
                }
                slots.Add(slot);
            }
        }

        if (slots.Count == 0) return;
        db.Appointments.AddRange(slots);
        db.SaveChanges();
    }

    private static Customer Customer(string id, string name, string phone, string email) =>
        new() { Id = id, Name = name, Phone = phone, Email = email };

    private static GownModel Model(int id, string name, string color, string description) =>
        new() { Id = id, Name = name, Color = color, Description = description };

    private static ModelDetails Details(int gownId, int modelId, int size, double price) =>
        new() { GownId = gownId, ModelId = modelId, Size = size, Price = price, IsActive = true };
}
