using Dal.Api;
using Dal.Models;
using Microsoft.EntityFrameworkCore;

namespace Dal.Services
{
    public class RentalService : IRentals
    {
        private readonly DataManager datamanager;

        public RentalService(DataManager datamanager)
        {
            this.datamanager = datamanager;
        }

        public Rental CreateRental(Rental r)
        {
            datamanager.Rentals.Add(r);
            datamanager.SaveChanges();
            return r;
        }

        public void DeleteRental(int id)
        {
            var rental = datamanager.Rentals.Find(id);
            if (rental != null)
            {
                datamanager.Rentals.Remove(rental);
                datamanager.SaveChanges();
            }
        }

        public Rental? GetRentalById(int id)
        {
            return datamanager.Rentals.Find(id);
        }

        public void UpdateRental(Rental r)
        {
            var oldRental = datamanager.Rentals.Find(r.Id)
                ?? throw new ArgumentNullException(nameof(r), "this rental doesn't exist. you can't update it.");
            oldRental.RentalDate = r.RentalDate;
            oldRental.EventDate = r.EventDate;
            oldRental.CustomerId = r.CustomerId;
            datamanager.SaveChanges();
        }

        public List<RentalWithDetails> GetRentalsWithDetails(int? rentalId = null, DateOnly? eventDate = null)
        {
            var query = datamanager.RentalDetails
                .AsNoTracking()
                .Join(datamanager.Rentals, d => d.RentalId, r => r.Id, (d, r) => new { Rental = r, Details = d });

            if (rentalId != null)
            {
                query = query.Where(x => x.Rental.Id == rentalId);
            }
            if (eventDate != null)
            {
                query = query.Where(x => x.Rental.EventDate == eventDate);
            }

            return query
                .OrderBy(x => x.Rental.Id)
                .AsEnumerable()
                .Select(x => new RentalWithDetails(x.Rental, x.Details))
                .ToList();
        }

        public List<int> GetRentedGownIds(DateOnly rangeStart, DateOnly rangeEnd)
        {
            return (from d in datamanager.RentalDetails
                    join r in datamanager.Rentals on d.RentalId equals r.Id
                    where r.EventDate >= rangeStart && r.EventDate <= rangeEnd
                    select d.GownId).Distinct().ToList();
        }
    }
}
