using Dal.Models;
using Dal.Api;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;

namespace Dal.Services
{
    public class RentalService : IRentals
    {
        DataManager datamanager;
        public RentalService(DataManager datamanager)
        {
            this.datamanager = datamanager;
        }
        public List<Rental> GetRentals()
        {
            return this.datamanager.Rentals.AsNoTracking().ToList();
        }


        public Rental createRental(Rental r)
        {
            this.datamanager.Rentals.Add(r);
            this.datamanager.SaveChanges();
            return r;
        }

        public void DeleteRental(int id)
        {
            Rental rental = datamanager.Rentals.ToList().Find(x =>
            {
                ArgumentNullException.ThrowIfNull(x);
                return x.Id == id;
            });
            if (rental != null)
            {
               this.datamanager.Rentals.Remove(rental);
               this.datamanager.SaveChanges();

            }

        }

        public Rental? GetRentalById(int id)
        {
            return datamanager.Rentals.Find(id);
        }

        public void updateRental(Rental r)
        {
            Rental oldRental = datamanager.Rentals.ToList().Find(x => x.Id == r.Id);
            if(oldRental == null)
            {
                throw new ArgumentNullException("this rental doesn't exist. you can't update it.");
            }
            oldRental.RentalDate = r.RentalDate;
            oldRental.EventDate = r.EventDate;
            oldRental.CustomerId = r.CustomerId;
            this.datamanager.SaveChanges();

        }

    }
}
