using Bl.Api;
using Bl.Services;
using Dal.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bl
{
    public class BLManager : IBl
    {
        public IBLRentals BLRentals { get; }
        public IBLGowns BLGowns { get; }
        public IBLAppointment BLAppointment { get; }
        public IBLAuth BLAuth { get; }
        public IBLOrders BLOrders { get; }


        public BLManager(IDal dal)
        {
            BLRentals = new BLRentalService(dal);
            BLGowns = new BLGownModelService(dal);
            BLAppointment = new BLAppointmentService(dal);
            BLAuth = new BLAuthService(dal);
            BLOrders = new BLOrderService(dal, BLRentals);
        }
    }
}
