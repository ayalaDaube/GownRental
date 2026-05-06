using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class ModelDetail
{
    public int GownId { get; set; }

    public int Size { get; set; }

    public int ModelId { get; set; }

    public double Price { get; set; }

    public bool IsAvailable { get; set; }

    public virtual GownModel Model { get; set; } = null!;
}
