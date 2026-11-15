using Dal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal.Api
{
    public interface IRentalDetails
    {
        List<RentalDetails> GetByRental(int rentalId);
        bool ExistsForGown(int gownId);
        /// <summary>The event date of a rental of this gown inside the date range, or null when there is none.</summary>
        DateOnly? GetConflictingEventDate(int gownId, DateOnly rangeStart, DateOnly rangeEnd, int? ignoreRentalId);
        bool ExistsForAnyGown(IEnumerable<int> gownIds);
        void DeleteRentalDetails(int id);
        void CreateRentalDetails(RentalDetails rd);
        void UpdateRentalDetails(RentalDetails rd);
    }
}
