using System;
using System.Collections.Generic;

namespace Dal.Models;

public partial class Rental
{
    public int Id { get; set; }

    public string? CustomerId { get; set; } 

    public DateTime RentalDate { get; set; }

    public DateOnly EventDate { get; set; }

}
