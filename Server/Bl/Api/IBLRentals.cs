using Bl.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bl.Api
{
    public interface IBLRentals
    {
        RentalFullDetails CreateRental(RentalFullDetails rental);
        List<RentalFullDetails> GetFullRentals();
        RentalFullDetails GetRentalById(int id);
        RentalFullDetails UpdateRental(RentalFullDetails rental);
        void DeleteRental(int id);
        List<RentalFullDetails> GetReturningRentals();
        List<RentalFullDetails> GetPickupRentals();
        
        List<int> GetRentedGownsByDate(DateOnly date);

    }
}
