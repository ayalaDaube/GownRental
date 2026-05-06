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
        void CreateRental(RentalFullDetaills rental);
        List<RentalFullDetaills> GetFullRentals();
        RentalFullDetaills GetRentalById(int id);
        void UpdateRental(RentalFullDetaills rental);
        void DeleteRental(int id);
        List<RentalFullDetaills> returningCustomers();
        List<RentalFullDetaills> rentingCustomers();
        


    }
}
