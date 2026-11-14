using Dal.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal.Api
{
    public interface IDal
    {
        IRentals Rentals { get; }
        IRentalDetails RentalDetails { get; }
        IGownDetails gownDetail { get; }
        IGownModel gownModel { get; }
        IAppointment Appointment { get; }
        ICustomer Customer { get; }
        IUser User { get; }

    }
}
