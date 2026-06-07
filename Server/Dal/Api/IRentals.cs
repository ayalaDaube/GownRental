
using Dal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal.Api
{
    public interface IRentals
    {
        List<Rental> GetRentals();
        Rental GetRentalById(int id);
        void DeleteRental(int id);
        Rental createRental(Rental r);
        void updateRental(Rental r);
    }
}
