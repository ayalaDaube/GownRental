using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class GownModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public byte[]? Image { get; set; }

    public string Color { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<ModelDetail> ModelDetails { get; set; } = new List<ModelDetail>();
}
