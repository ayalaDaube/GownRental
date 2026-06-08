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

        public BLManager(IDal dal)
        {
            BLRentals = new BLRentalService(dal);
            BLGowns = new BLGownModelService(dal);
        }
    }
}
