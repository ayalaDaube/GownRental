using System;
using System.Collections.Generic;

namespace Dal.Models;

public partial class Rental
{
    public int Id { get; set; }

    public string CustomerId { get; set; } = null!;

    public DateOnly RentalDate { get; set; }

    public DateOnly EventDate { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<RentalDetails> RentalDetails { get; set; } = new List<RentalDetails>();
}
