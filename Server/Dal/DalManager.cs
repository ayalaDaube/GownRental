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

        public IAppointment Appointment { get; }

        public ICustomer Customer { get; }

        public IUser User { get; }

        public DalManager(IRentals rentals, IRentalDetails rentalDetails, IGownModel gownModel, IGownDetails gownDetail, IAppointment appointment, ICustomer customer, IUser user)
        {
            Rentals = rentals;
            RentalDetails = rentalDetails;
            this.gownModel = gownModel;
            this.gownDetail = gownDetail;
            this.Appointment = appointment;
            this.Customer = customer;
            this.User = user;
        }
    }
}
