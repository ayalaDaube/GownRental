using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bl.Api
{
    public interface IBl
    {
        public IBLRentals BLRentals { get; }

        public IBLGowns BLGowns { get; }

        public IBLAppointment? BLAppointment { get; }

        public IBLAuth BLAuth { get; }
    }
}
