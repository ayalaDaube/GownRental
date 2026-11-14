using Bl.Api;
using Bl.Exceptions;
using Bl.Models;
using Dal.Api;
using Dal.Models;

namespace Bl.Services
{
    public class BLRentalService : IBLRentals
    {
        private readonly IDal dal;
        private readonly IRentals rentals;
        private readonly IRentalDetails rentalDetails;
        private readonly IGownDetails gowns;
        private readonly CustomerRegistrar registrar;

        public BLRentalService(IDal dal)
        {
            this.dal = dal;
            rentals = dal.Rentals;
            rentalDetails = dal.RentalDetails;
            gowns = dal.gownDetail;
            registrar = new CustomerRegistrar(dal.Customer);
        }

        private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

        /// <summary>
        /// Creates a rental of one gown. Everything is checked first and saved together:
        /// the customer and the gown must exist, the gown must be in the inventory, and the same
        /// gown must not be rented for another event within the buffer days.
        /// </summary>
        public RentalFullDetaills CreateRental(RentalFullDetaills request)
        {
            if (request == null)
            {
                throw new InvalidRequestException("Rental details are required.");
            }

            return dal.RunInTransaction(() =>
            {
                var gown = Validate(request, ignoreRentalId: null, requireFutureDate: true, requireActiveGown: true);

                var created = rentals.createRental(new Rental
                {
                    CustomerId = CustomerRules.NormalizeId(request.customerId),
                    EventDate = request.eventDate,
                    RentalDate = DateTime.Now, // the booking time is decided here, not by the caller
                });
                rentalDetails.createRentalDetails(new RentalDetails
                {
                    RentalId = created.Id,
                    GownId = gown.GownId,
                    Size = gown.Size,
                });

                return GetRentalById(created.Id);
            });
        }

        public List<RentalFullDetaills> GetFullRentals()
        {
            return BuildRentals().ToList();
        }

        public RentalFullDetaills GetRentalById(int id)
        {
            return BuildRentals().FirstOrDefault(r => r.rentalId == id)
                   ?? throw new NotFoundException($"Rental {id} does not exist.");
        }

        /// <summary>Changes the customer, event date or gown of a rental. The booking time is kept.</summary>
        public RentalFullDetaills UpdateRental(RentalFullDetaills request)
        {
            if (request == null)
            {
                throw new InvalidRequestException("Rental details are required.");
            }

            return dal.RunInTransaction(() =>
            {
                var existing = rentals.GetRentalById(request.rentalId)
                    ?? throw new NotFoundException($"Rental {request.rentalId} does not exist.");
                var existingDetails = rentalDetails.GetByRental(existing.Id).FirstOrDefault()
                    ?? throw new NotFoundException($"Rental {existing.Id} has no gown.");

                var dateChanged = request.eventDate != existing.EventDate;
                var gownChanged = request.gownId != existingDetails.GownId;
                var gown = Validate(request, ignoreRentalId: existing.Id,
                    requireFutureDate: dateChanged, requireActiveGown: gownChanged);

                rentals.updateRental(new Rental
                {
                    Id = existing.Id,
                    CustomerId = CustomerRules.NormalizeId(request.customerId),
                    EventDate = request.eventDate,
                    RentalDate = existing.RentalDate,
                });
                rentalDetails.updateRentalDetails(new RentalDetails
                {
                    Id = existingDetails.Id,
                    RentalId = existing.Id,
                    GownId = gown.GownId,
                    Size = gown.Size,
                });

                return GetRentalById(existing.Id);
            });
        }

        public void DeleteRental(int id)
        {
            dal.RunInTransaction(() =>
            {
                if (rentals.GetRentalById(id) == null)
                {
                    throw new NotFoundException($"Rental {id} does not exist.");
                }

                foreach (var details in rentalDetails.GetByRental(id))
                {
                    rentalDetails.DeleteRentalDetails(details.Id);
                }
                rentals.DeleteRental(id);
            });
        }

        /// <summary>Rentals whose event was the buffer days ago: the gowns that come back to the shop.</summary>
        public List<RentalFullDetaills> returningCustomers()
        {
            var day = Today.AddDays(-RentalRules.BufferDays);
            return BuildRentals().Where(r => r.eventDate == day).ToList();
        }

        /// <summary>Rentals whose event is in the buffer days: the gowns that customers pick up now.</summary>
        public List<RentalFullDetaills> rentingCustomers()
        {
            var day = Today.AddDays(RentalRules.BufferDays);
            return BuildRentals().Where(r => r.eventDate == day).ToList();
        }

        /// <summary>The ids of the gowns that are rented within the buffer days around the date.</summary>
        public List<int> GetRentedGownsByDate(DateOnly date)
        {
            if (date < Today)
            {
                throw new InvalidRequestException("The date cannot be in the past.");
            }

            var rangeStart = date.AddDays(-RentalRules.BufferDays);
            var rangeEnd = date.AddDays(RentalRules.BufferDays);

            return (from r in rentals.GetRentals()
                    join rd in rentalDetails.GetRentalDetails() on r.Id equals rd.RentalId
                    where r.EventDate >= rangeStart && r.EventDate <= rangeEnd
                    select rd.GownId).Distinct().ToList();
        }

        /// <summary>Checks the rules shared by creating and updating a rental and returns the gown that is rented.</summary>
        private ModelDetails Validate(RentalFullDetaills request, int? ignoreRentalId, bool requireFutureDate, bool requireActiveGown)
        {
            var customerId = CustomerRules.NormalizeId(request.customerId);
            if (request.gownId <= 0)
            {
                throw new InvalidRequestException("A gown id is required.");
            }
            if (requireFutureDate && request.eventDate < Today)
            {
                throw new InvalidRequestException("The event date cannot be in the past.");
            }

            var gown = gowns.GetGownDetailsById(request.gownId)
                ?? throw new NotFoundException($"Gown {request.gownId} does not exist.");
            if (requireActiveGown && !gown.IsActive)
            {
                throw new ConflictException($"Gown {gown.GownId} is out of stock.");
            }
            // The size is a property of the gown. It may be left out, but it must not contradict the gown.
            if (request.size != 0 && request.size != gown.Size)
            {
                throw new InvalidRequestException($"Gown {gown.GownId} is size {gown.Size}, not size {request.size}.");
            }

            var clash = rentalDetails.GetConflictingEventDate(
                gown.GownId,
                request.eventDate.AddDays(-RentalRules.BufferDays),
                request.eventDate.AddDays(RentalRules.BufferDays),
                ignoreRentalId);
            if (clash != null)
            {
                throw new ConflictException(
                    $"Gown {gown.GownId} is already rented for an event on {clash:yyyy-MM-dd}. " +
                    $"Events for the same gown must be more than {RentalRules.BufferDays} days apart.");
            }

            // Last, so that nothing is written for a rental that is going to be refused anyway.
            registrar.EnsureExists(customerId, request.customerName, request.customerPhone, request.customerEmail);

            return gown;
        }

        private IEnumerable<RentalFullDetaills> BuildRentals()
        {
            return from r in rentals.GetRentals()
                   join rd in rentalDetails.GetRentalDetails() on r.Id equals rd.RentalId
                   select new RentalFullDetaills
                   {
                       customerId = r.CustomerId,
                       rentalDate = r.RentalDate,
                       eventDate = r.EventDate,
                       rentalId = rd.RentalId,
                       size = rd.Size,
                       gownId = rd.GownId,
                   };
        }
    }
}
