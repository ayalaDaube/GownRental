using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bl.Models
{
    public class RentalFullDetails
    {
        public string CustomerId { get; set; }
        public DateTime RentalDate { get; set; }
        public DateOnly EventDate { get; set; }
        public int RentalId { get; set; }
        public int Size { get; set; }
        public int GownId { get; set; }

        // Used only when the customer is new: they are registered with these details. Never filled in responses.
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
    }
}
