using Dal.Models;

namespace Dal.Api
{
    public interface IRentals
    {
        Rental? GetRentalById(int id);
        void DeleteRental(int id);
        Rental CreateRental(Rental r);
        void UpdateRental(Rental r);

        /// <summary>
        /// Rentals joined with their gown lines, filtered by the database: by rental id and/or by event date.
        /// Leave both out to get every rental.
        /// </summary>
        List<RentalWithDetails> GetRentalsWithDetails(int? rentalId = null, DateOnly? eventDate = null);

        /// <summary>The ids of the gowns rented for an event in the date range (both ends included).</summary>
        List<int> GetRentedGownIds(DateOnly rangeStart, DateOnly rangeEnd);
    }
}
