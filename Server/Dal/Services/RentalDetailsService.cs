using Dal.Api;
using Dal.Models;
using Microsoft.EntityFrameworkCore;

namespace Dal.Services
{
    public class RentalDetailsService : IRentalDetails
    {
        private readonly DataManager datamanager;

        public RentalDetailsService(DataManager datamanager)
        {
            this.datamanager = datamanager;
        }

        public List<RentalDetails> GetByRental(int rentalId)
        {
            return datamanager.RentalDetails.Where(d => d.RentalId == rentalId).ToList();
        }

        public DateOnly? GetConflictingEventDate(int gownId, DateOnly rangeStart, DateOnly rangeEnd, int? ignoreRentalId)
        {
            return (from d in datamanager.RentalDetails
                    join r in datamanager.Rentals on d.RentalId equals r.Id
                    where d.GownId == gownId
                          && r.EventDate >= rangeStart && r.EventDate <= rangeEnd
                          && (ignoreRentalId == null || r.Id != ignoreRentalId)
                    orderby r.EventDate
                    select (DateOnly?)r.EventDate).FirstOrDefault();
        }

        public bool ExistsForGown(int gownId)
        {
            return datamanager.RentalDetails.Any(d => d.GownId == gownId);
        }

        public bool ExistsForAnyGown(IEnumerable<int> gownIds)
        {
            var ids = gownIds.ToList();
            return datamanager.RentalDetails.Any(d => ids.Contains(d.GownId));
        }

        public void CreateRentalDetails(RentalDetails rd)
        {
            datamanager.RentalDetails.Add(rd);
            datamanager.SaveChanges();
        }

        public void DeleteRentalDetails(int id)
        {
            var rentalDetails = datamanager.RentalDetails.Find(id);
            if (rentalDetails != null)
            {
                datamanager.RentalDetails.Remove(rentalDetails);
                datamanager.SaveChanges();
            }
        }

        public void UpdateRentalDetails(RentalDetails rd)
        {
            var old = datamanager.RentalDetails.Find(rd.Id)
                ?? throw new ArgumentNullException(nameof(rd), "this rental doesn't exist. you can't update it.");
            old.RentalId = rd.RentalId;
            old.GownId = rd.GownId;
            old.Size = rd.Size;
            datamanager.SaveChanges();
        }
    }
}
