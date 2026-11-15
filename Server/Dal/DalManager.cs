using Dal.Api;
using Dal.Models;
using Microsoft.EntityFrameworkCore;
using Dal.Services;

namespace Dal
{
    public class DalManager : IDal
    {
        public IRentalDetails RentalDetails { get; }

        public IRentals Rentals { get; }
        
        public IGownDetails GownDetails { get; }

        public IGownModel GownModels { get; }

        public IAppointment Appointment { get; }

        public ICustomer Customer { get; }

        public IUser User { get; }

        private readonly DataManager dataManager;

        public DalManager(DataManager dataManager, IRentals rentals, IRentalDetails rentalDetails, IGownModel gownModels, IGownDetails gownDetails, IAppointment appointment, ICustomer customer, IUser user)
        {
            this.dataManager = dataManager;
            Rentals = rentals;
            RentalDetails = rentalDetails;
            GownModels = gownModels;
            GownDetails = gownDetails;
            Appointment = appointment;
            Customer = customer;
            User = user;
        }

        public void RunInTransaction(Action work)
        {
            RunInTransaction<object?>(() =>
            {
                work();
                return null;
            });
        }

        public T RunInTransaction<T>(Func<T> work)
        {
            // Already inside a transaction (a nested call): simply take part in it.
            if (dataManager.Database.CurrentTransaction != null)
            {
                return work();
            }

            using var transaction = dataManager.Database.BeginTransaction();
            var result = work();
            transaction.Commit(); // leaving without Commit (because of an exception) rolls everything back
            return result;
        }
    }
}
