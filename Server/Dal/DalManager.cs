using Dal.Api;
using Dal.Models;
using Dal.Services;

namespace Dal
{
    public class DalManager : IDal
    {
        public IRentalDetails RentalDetails { get; }

        public IRentals Rentals { get; }
        
        public IGownDetails gownDetail { get; }

        public IGownModel gownModel { get; }

        public DalManager(IRentals rentals, IRentalDetails rentalDetails, IGownModel gownModel, IGownDetails gownDetail)
        {
            Rentals = rentals;
            RentalDetails = rentalDetails;
            this.gownModel = gownModel;
            this.gownDetail = gownDetail;
        }
    }
}
