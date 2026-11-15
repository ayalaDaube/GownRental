using Dal.Api;
using Dal.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal.Services
{
    public class RentalDetailsService :IRentalDetails
    {
        DataManager datamanager;
        public RentalDetailsService(DataManager datamanager)
        {
            this.datamanager = datamanager;
        }
        public List<RentalDetails> GetRentalDetails()
        {
            return this.datamanager.RentalDetails.AsNoTracking().ToList();
        }


        public List<RentalDetails> GetByRental(int rentalId)
        {
            return this.datamanager.RentalDetails.Where(d => d.RentalId == rentalId).ToList();
        }

        public DateOnly? GetConflictingEventDate(int gownId, DateOnly rangeStart, DateOnly rangeEnd, int? ignoreRentalId)
        {
            return (from d in this.datamanager.RentalDetails
                    join r in this.datamanager.Rentals on d.RentalId equals r.Id
                    where d.GownId == gownId
                          && r.EventDate >= rangeStart && r.EventDate <= rangeEnd
                          && (ignoreRentalId == null || r.Id != ignoreRentalId)
                    orderby r.EventDate
                    select (DateOnly?)r.EventDate).FirstOrDefault();
        }

        public bool ExistsForGown(int gownId)
        {
            return this.datamanager.RentalDetails.Any(d => d.GownId == gownId);
        }

        public bool ExistsForAnyGown(IEnumerable<int> gownIds)
        {
            var ids = gownIds.ToList();
            return this.datamanager.RentalDetails.Any(d => ids.Contains(d.GownId));
        }

        public void CreateRentalDetails(RentalDetails rd)
        {
            this.datamanager.RentalDetails.Add(rd);
            this.datamanager.SaveChanges();
        }

        public void DeleteRentalDetails(int id)
        {
            RentalDetails rentalDetails = datamanager.RentalDetails.ToList().Find(x =>
            {
                ArgumentNullException.ThrowIfNull(x);
                return x.Id == id;
            });
            if (rentalDetails != null)
            {
                this.datamanager.RentalDetails.Remove(rentalDetails);
                this.datamanager.SaveChanges();
            }
        }

        public RentalDetails GetRentalDetailsById(int id)
        {

            RentalDetails rentalDetails = datamanager.RentalDetails.ToList().Find(x => x.Id == id);
            if (rentalDetails == null)
            {
                throw new ArgumentNullException("These rental details doesn't exist");
            }
            return rentalDetails;
        }

        public void UpdateRentalDetails(RentalDetails rd)
        {
            RentalDetails oldRentalDetails = datamanager.RentalDetails.ToList().Find(x => x.Id == rd.Id);
            if (oldRentalDetails == null)
            {
                throw new ArgumentNullException("this rental doesn't exist. you can't update it.");
            }
            oldRentalDetails.RentalId = rd.RentalId;
            oldRentalDetails.GownId = rd.GownId;
            oldRentalDetails.Size = rd.Size;
            this.datamanager.SaveChanges();
        }
    }
}
