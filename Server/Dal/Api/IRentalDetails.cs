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
        List<RentalDetails> GetRentalDetails();
        RentalDetails GetRentalDetailsById(int id);
        bool ExistsForGown(int gownId);
        bool ExistsForAnyGown(IEnumerable<int> gownIds);
        void DeleteRentalDetails(int id);
        void createRentalDetails(RentalDetails rd);
        void updateRentalDetails(RentalDetails rd);
    }
}
