using Dal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bl.Models
{
    public class AppointmentFullDetaills
    {
        public int Id { get; set; }
        public DateOnly AppointmentDate { get; set; }

        public string CustomerId { get; set; } = null!;

        public TimeOnly Hour { get; set; }

        public int? NumberOfPeople { get; set; }
        public bool? IsAvailable { get; set; } = false;

        public string CustomerName { get; set; } = null!;

        public string CustomerPhone { get; set; } = null!;

        public string? CustomerEmail { get; set; }

      
    }
}
