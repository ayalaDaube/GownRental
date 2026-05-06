using Bl.Api;
using Bl.Models;
using Dal.Api;
using Dal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bl.Services
{
    public class BLRentalService : IBLRentals
    {
        IRentalDetails rentalDetaills;
        IRentals rental;
        public BLRentalService(IDal Rental)
        {
            rental = Rental.Rentals;
            rentalDetaills = Rental.RentalDetails;
        }
        public void CreateRental(RentalFullDetaills fullRental)
        {
            if(fullRental == null)
            {
                throw new ArgumentNullException("Rental details cannot be null");
            }
            if(string.IsNullOrEmpty(fullRental.customerId))
            {
                throw new ArgumentException("Customer ID cannot be null or empty");
            }
            if(fullRental.size <= 0||fullRental.size==null)
            {
                throw new ArgumentException("Size is expected, must be greater than zero");
            }
            if(fullRental.gownId <= 0 || fullRental.gownId == null)
            {
                throw new ArgumentException("Gown ID is expected, must be greater than zero");
            }
            if (fullRental.eventDate < DateOnly.FromDateTime(DateTime.Now))
            {
                throw new ArgumentException("Event date cannot be in the past");
            }
            var newRental = new Rental()
            {
                CustomerId = fullRental.customerId,
                EventDate = fullRental.eventDate,
                RentalDate = DateTime.Now
            };

            var createdRental = rental.createRental(newRental);

            var newRentalDetails = new RentalDetails()
            {
                RentalId = createdRental.Id,
                Size = fullRental.size,
                GownId = fullRental.gownId
            };

            rentalDetaills.createRentalDetails(newRentalDetails);
        }


        public List<RentalFullDetaills> GetFullRentals()
        {
            if(rental.GetRentals() == null || rentalDetaills.GetRentalDetails() == null)
            {
                throw new ArgumentNullException("No rentals or rental details found");
            }
            return (from r in rental.GetRentals()
                    join rd in rentalDetaills.GetRentalDetails() on r.Id equals rd.RentalId
                    select new RentalFullDetaills()
                    {
                        customerId = r.CustomerId,
                        rentalDate = r.RentalDate,
                        eventDate = r.EventDate,
                        rentalId = rd.RentalId,
                        size = rd.Size,
                        gownId = rd.GownId
                    }).ToList();
        }

        public RentalFullDetaills? GetRentalById(int id)
        {
            if(id < 0||id==null)
            {
                throw new ArgumentException("Rental ID is expected and must be a positive integer");
            }
            var rentalById= (from r in rental.GetRentals()
                    join rd in rentalDetaills.GetRentalDetails() on r.Id equals rd.RentalId
                    where r.Id == id
                    select new RentalFullDetaills()
                    {
                        customerId = r.CustomerId,
                        rentalDate = r.RentalDate,
                        eventDate = r.EventDate,
                        rentalId = rd.RentalId,
                        size = rd.Size,
                        gownId = rd.GownId
                    }).FirstOrDefault();
            if(rentalById == null)
            {
                throw new ArgumentNullException("Rental not found for the given ID");
            }
            return rentalById;
        }

        public void UpdateRental(RentalFullDetaills updatedRental)
        {
            if (updatedRental == null)
            {
                throw new ArgumentNullException("Rental details cannot be null");
            }
            if (string.IsNullOrEmpty(updatedRental.customerId))
            {
                throw new ArgumentException("Customer ID cannot be null or empty");
            }
            if (updatedRental.size <= 0 || updatedRental.size == null)
            {
                throw new ArgumentException("Size is expected, must be greater than zero");
            }
            if (updatedRental.gownId <= 0 || updatedRental.gownId == null)
            {
                throw new ArgumentException("Gown ID is expected, must be greater than zero");
            }
            if (updatedRental.eventDate < DateOnly.FromDateTime(DateTime.Now))
            {
                throw new ArgumentException("Event date cannot be in the past");
            }
            this.rental.updateRental(new Rental()
            {
                CustomerId = updatedRental.customerId,
                EventDate = updatedRental.eventDate,
                RentalDate = updatedRental.rentalDate
            });
            var existingDetails = rentalDetaills.GetRentalDetails().FirstOrDefault(rd => rd.RentalId == updatedRental.rentalId);
            if (existingDetails == null)
                throw new ArgumentNullException("RentalDetails not found for this rental");
            this.rentalDetaills.updateRentalDetails(new RentalDetails()
            {
                Id = existingDetails.Id,
                RentalId = updatedRental.rentalId,
                Size = updatedRental.size,
                GownId = updatedRental.gownId
            });
        }



        public void DeleteRental(int id)
        {
           if(id < 0 || id == null)
            {
                throw new ArgumentException("Rental ID is expected and must be a positive integer");
            }
            RentalDetails oldRental = rentalDetaills.GetRentalDetailsById(id);
            
            if (oldRental == null)
            {
                throw new ArgumentNullException("This rental details doesn't exist");
            }
           
            this.rentalDetaills.DeleteRentalDetails(oldRental.Id);
            this.rental.DeleteRental(id);


        }

        public List<RentalFullDetaills> returningCustomers()
        {
            DateTime currentDate = DateTime.Now;
            currentDate=currentDate.AddDays(3);
            var rentalNeeded = (from r in rental.GetRentals()
                    join rd in rentalDetaills.GetRentalDetails() on r.Id equals rd.RentalId
                    where r.EventDate.Year == currentDate.Year &&
                          r.EventDate.Month == currentDate.Month &&
                          r.EventDate.Day == currentDate.Day
                    select new RentalFullDetaills()
                    {
                        customerId = r.CustomerId,
                        rentalDate = r.RentalDate,
                        eventDate = r.EventDate,
                        rentalId = rd.RentalId,
                        size = rd.Size,
                        gownId = rd.GownId
                    }).ToList();
            if (rentalNeeded == null)
            {
                throw new ArgumentNullException("No rentals found for the upcoming event date");
            }
            return rentalNeeded;
        }

        public List<RentalFullDetaills> rentingCustomers()
        {
            DateTime currentDate = DateTime.Now;
            currentDate= currentDate.AddDays(-3);
            var rentalNeeded = (from r in rental.GetRentals()
                    join rd in rentalDetaills.GetRentalDetails() on r.Id equals rd.RentalId
                    where r.EventDate.Year == currentDate.Year &&
                          r.EventDate.Month == currentDate.Month &&
                          r.EventDate.Day == currentDate.Day
                    select new RentalFullDetaills()
                    {
                        customerId = r.CustomerId,
                        rentalDate = r.RentalDate,
                        eventDate = r.EventDate,
                        rentalId = rd.RentalId,
                        size = rd.Size,
                        gownId = rd.GownId
                    }).ToList();
            if (rentalNeeded == null)
            {
                throw new ArgumentNullException("No rentals found for the recent event date");
            }
            return rentalNeeded;
        }
    }
}
