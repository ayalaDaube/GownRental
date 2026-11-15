using System.ComponentModel.DataAnnotations;

namespace Bl.Models
{
    /// <summary>One customer renting one or more gowns for the same event.</summary>
    public class OrderRequest
    {
        [Required, StringLength(20)]
        public string CustomerId { get; set; } = string.Empty;

        // Needed only when the customer is not registered yet.
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        public DateOnly EventDate { get; set; }

        [Required, MinLength(1, ErrorMessage = "Choose at least one gown."),
         MaxLength(20, ErrorMessage = "An order can have at most 20 gowns.")]
        public List<int> GownIds { get; set; } = new();
    }

    public class OrderResult
    {
        public string CustomerId { get; set; } = string.Empty;

        public DateOnly EventDate { get; set; }

        /// <summary>One rental per gown in the order.</summary>
        public List<RentalFullDetails> Rentals { get; set; } = new();
    }
}
