using Dal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bl.Models
{
    public class AppointmentFullDetails
    {
        public int Id { get; set; }
        public DateOnly AppointmentDate { get; set; }

        public string CustomerId { get; set; } = null!;

        public TimeOnly Hour { get; set; }

        public int? NumberOfPeople { get; set; }
        public bool? IsAvailable { get; set; } = false;

        // Needed only when the customer is not registered yet; the server then checks them.
        public string? CustomerName { get; set; }

        public string? CustomerPhone { get; set; }

        public string? CustomerEmail { get; set; }

      
    }
}
