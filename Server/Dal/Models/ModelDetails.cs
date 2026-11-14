using System;
using System.Collections.Generic;

namespace Dal.Models;

public partial class ModelDetails
{
    public int GownId { get; set; }

    public int Size { get; set; }

    public int ModelId { get; set; }

    public double Price { get; set; }
  
    /// <summary>True while the gown is part of the inventory. Whether it is free on a given date is worked out from the rentals.</summary>
    public bool IsActive { get; set; } = true;

    public virtual GownModel Model { get; set; } = null!;
}
