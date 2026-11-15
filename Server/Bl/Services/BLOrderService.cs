using Bl.Api;
using Bl.Exceptions;
using Bl.Models;
using Dal.Api;

namespace Bl.Services
{
    public class BLOrderService : IBLOrders
    {
        private readonly IDal dal;
        private readonly IBLRentals rentals;

        public BLOrderService(IDal dal, IBLRentals rentals)
        {
            this.dal = dal;
            this.rentals = rentals;
        }

        /// <summary>
        /// All gowns are rented in one database transaction. If any gown cannot be rented (unknown, out of stock,
        /// already rented around the event, invalid customer...), the exception stops the whole order and
        /// nothing is saved, so an order is never half placed.
        /// </summary>
        public OrderResult PlaceOrder(OrderRequest request)
        {
            if (request == null)
            {
                throw new InvalidRequestException("The order is required.");
            }
            if (request.GownIds == null || request.GownIds.Count == 0)
            {
                throw new InvalidRequestException("Choose at least one gown.");
            }

            var duplicate = request.GownIds.GroupBy(id => id).FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
            {
                throw new InvalidRequestException($"Gown {duplicate.Key} is in the order more than once.");
            }

            return dal.RunInTransaction(() =>
            {
                var created = request.GownIds
                    .Select(gownId => rentals.CreateRental(new RentalFullDetails
                    {
                        CustomerId = request.CustomerId,
                        CustomerName = request.CustomerName,
                        CustomerPhone = request.CustomerPhone,
                        CustomerEmail = request.CustomerEmail,
                        EventDate = request.EventDate,
                        GownId = gownId,
                    }))
                    .ToList();

                return new OrderResult
                {
                    CustomerId = created[0].CustomerId,
                    EventDate = request.EventDate,
                    Rentals = created,
                };
            });
        }
    }
}
