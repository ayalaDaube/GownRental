using System;
using System.Collections.Generic;

namespace Dal.Models;

public partial class ModelDetails
{
    public int GownId { get; set; }

    public int Size { get; set; }

    public int ModelId { get; set; }

    public double Price { get; set; }
  
    public bool IsAvailable { get; set; } = true;

    public virtual GownModel Model { get; set; } = null!;
}
