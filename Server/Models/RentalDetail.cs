using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class RentalDetail
{
    public int Id { get; set; }

    public int RentalId { get; set; }

    public int Size { get; set; }

    public int GownId { get; set; }

    public virtual Rental Rental { get; set; } = null!;
}
