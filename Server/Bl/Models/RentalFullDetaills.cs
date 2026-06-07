using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bl.Models
{
    public class RentalFullDetaills
    {
        public string customerId { get; set; }
        public DateTime rentalDate { get; set; }
        public DateOnly eventDate { get; set; }
        public int rentalId { get; set; }
        public int size { get; set; }
        public int gownId { get; set; }
    }
}
