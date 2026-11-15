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
            gowns = dal.GownDetails;
            registrar = new CustomerRegistrar(dal.Customer);
        }

        private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

        /// <summary>
        /// Creates a rental of one gown. Everything is checked first and saved together:
        /// the customer and the gown must exist, the gown must be in the inventory, and the same
        /// gown must not be rented for another event within the buffer days.
        /// </summary>
        public RentalFullDetails CreateRental(RentalFullDetails request)
        {
            if (request == null)
            {
                throw new InvalidRequestException("Rental details are required.");
            }

            return dal.RunInTransaction(() =>
            {
                var gown = Validate(request, ignoreRentalId: null, requireFutureDate: true, requireActiveGown: true);

                var created = rentals.CreateRental(new Rental
                {
                    CustomerId = CustomerRules.NormalizeId(request.CustomerId),
                    EventDate = request.EventDate,
                    RentalDate = DateTime.Now, // the booking time is decided here, not by the caller
                });
                rentalDetails.CreateRentalDetails(new RentalDetails
                {
                    RentalId = created.Id,
                    GownId = gown.GownId,
                    Size = gown.Size,
                });

                return GetRentalById(created.Id);
            });
        }

        public List<RentalFullDetails> GetFullRentals()
        {
            return BuildRentals().ToList();
        }

        public RentalFullDetails GetRentalById(int id)
        {
            return BuildRentals(rentalId: id).FirstOrDefault()
                   ?? throw new NotFoundException($"Rental {id} does not exist.");
        }

        /// <summary>Changes the customer, event date or gown of a rental. The booking time is kept.</summary>
        public RentalFullDetails UpdateRental(RentalFullDetails request)
        {
            if (request == null)
            {
                throw new InvalidRequestException("Rental details are required.");
            }

            return dal.RunInTransaction(() =>
            {
                var existing = rentals.GetRentalById(request.RentalId)
                    ?? throw new NotFoundException($"Rental {request.RentalId} does not exist.");
                var existingDetails = rentalDetails.GetByRental(existing.Id).FirstOrDefault()
                    ?? throw new NotFoundException($"Rental {existing.Id} has no gown.");

                var dateChanged = request.EventDate != existing.EventDate;
                var gownChanged = request.GownId != existingDetails.GownId;
                var gown = Validate(request, ignoreRentalId: existing.Id,
                    requireFutureDate: dateChanged, requireActiveGown: gownChanged);

                rentals.UpdateRental(new Rental
                {
                    Id = existing.Id,
                    CustomerId = CustomerRules.NormalizeId(request.CustomerId),
                    EventDate = request.EventDate,
                    RentalDate = existing.RentalDate,
                });
                rentalDetails.UpdateRentalDetails(new RentalDetails
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
        public List<RentalFullDetails> GetReturningRentals()
        {
            var day = Today.AddDays(-RentalRules.BufferDays);
            return BuildRentals(eventDate: day).ToList();
        }

        /// <summary>Rentals whose event is in the buffer days: the gowns that customers pick up now.</summary>
        public List<RentalFullDetails> GetPickupRentals()
        {
            var day = Today.AddDays(RentalRules.BufferDays);
            return BuildRentals(eventDate: day).ToList();
        }

        /// <summary>The ids of the gowns that are rented within the buffer days around the date.</summary>
        public List<int> GetRentedGownsByDate(DateOnly date)
        {
            if (date < Today)
            {
                throw new InvalidRequestException("The date cannot be in the past.");
            }
            RentalRules.EnsureNotTooFarAhead(date, Today);

            var rangeStart = date.AddDays(-RentalRules.BufferDays);
            var rangeEnd = date.AddDays(RentalRules.BufferDays);

            return rentals.GetRentedGownIds(rangeStart, rangeEnd);
        }

        /// <summary>Checks the rules shared by creating and updating a rental and returns the gown that is rented.</summary>
        private ModelDetails Validate(RentalFullDetails request, int? ignoreRentalId, bool requireFutureDate, bool requireActiveGown)
        {
            var customerId = CustomerRules.NormalizeId(request.CustomerId);
            if (request.GownId <= 0)
            {
                throw new InvalidRequestException("A gown id is required.");
            }
            if (requireFutureDate)
            {
                if (request.EventDate < Today)
                {
                    throw new InvalidRequestException("The event date cannot be in the past.");
                }
                RentalRules.EnsureNotTooFarAhead(request.EventDate, Today);
            }

            var gown = gowns.GetGownDetailsById(request.GownId)
                ?? throw new NotFoundException($"Gown {request.GownId} does not exist.");
            if (requireActiveGown && !gown.IsActive)
            {
                throw new ConflictException($"Gown {gown.GownId} is out of stock.");
            }
            // The size is a property of the gown. It may be left out, but it must not contradict the gown.
            if (request.Size != 0 && request.Size != gown.Size)
            {
                throw new InvalidRequestException($"Gown {gown.GownId} is size {gown.Size}, not size {request.Size}.");
            }

            var clash = rentalDetails.GetConflictingEventDate(
                gown.GownId,
                request.EventDate.AddDays(-RentalRules.BufferDays),
                request.EventDate.AddDays(RentalRules.BufferDays),
                ignoreRentalId);
            if (clash != null)
            {
                throw new ConflictException(
                    $"Gown {gown.GownId} is already rented for an event on {clash:yyyy-MM-dd}. " +
                    $"Events for the same gown must be more than {RentalRules.BufferDays} days apart.");
            }

            // Last, so that nothing is written for a rental that is going to be refused anyway.
            registrar.EnsureExists(customerId, request.CustomerName, request.CustomerPhone, request.CustomerEmail);

            return gown;
        }

        /// <summary>The rentals with their gown, read with the filter applied by the database.</summary>
        private IEnumerable<RentalFullDetails> BuildRentals(int? rentalId = null, DateOnly? eventDate = null)
        {
            return rentals.GetRentalsWithDetails(rentalId, eventDate)
                .Select(x => new RentalFullDetails
                {
                    CustomerId = x.Rental.CustomerId,
                    RentalDate = x.Rental.RentalDate,
                    EventDate = x.Rental.EventDate,
                    RentalId = x.Rental.Id,
                    Size = x.Details.Size,
                    GownId = x.Details.GownId,
                });
        }
    }
}
