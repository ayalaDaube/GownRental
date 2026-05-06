using System;
using System.Collections.Generic;

namespace Dal.Models;

public partial class ModelDetails
{
    public double GownId { get; set; }

    public int Size { get; set; }

    public int ModelId { get; set; }

    public double Price { get; set; }

    public virtual GownModel Model { get; set; } = null!;
}
