using System;
using System.Collections.Generic;

namespace Dal.Models;

public partial class RentalDetails
{
    public int Id { get; set; }

    public int RentalId { get; set; }

    public int Size { get; set; }

    public int GownId { get; set; }

}
