using Dal.Api;
using Dal.Models;
using Microsoft.EntityFrameworkCore;
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

        private readonly DataManager dataManager;

        public DalManager(DataManager dataManager, IRentals rentals, IRentalDetails rentalDetails, IGownModel gownModel, IGownDetails gownDetail, IAppointment appointment, ICustomer customer, IUser user)
        {
            this.dataManager = dataManager;
            Rentals = rentals;
            RentalDetails = rentalDetails;
            this.gownModel = gownModel;
            this.gownDetail = gownDetail;
            this.Appointment = appointment;
            this.Customer = customer;
            this.User = user;
        }

        public void RunInTransaction(Action work)
        {
            // Already inside a transaction (a nested call): simply take part in it.
            if (dataManager.Database.CurrentTransaction != null)
            {
                work();
                return;
            }

            using var transaction = dataManager.Database.BeginTransaction();
            work();
            transaction.Commit(); // leaving without Commit (because of an exception) rolls everything back
        }
    }
}
