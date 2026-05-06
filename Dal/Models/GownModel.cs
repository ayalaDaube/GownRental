using System;
using System.Collections.Generic;

namespace Dal.Models;

public partial class GownModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public byte[]? Image { get; set; }

    public string Color { get; set; } = null!;

    public string? Description { get; set; }
  
    public bool isAvailable { get; set; }

    public virtual ICollection<ModelDetails> ModelDetails { get; set; } = new List<ModelDetails>();
}
