using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class Appointment
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public string CustomerId { get; set; } = null!;

    public TimeOnly Hour { get; set; }

    public int? NumberOfPeople { get; set; }

    public virtual Customer Customer { get; set; } = null!;
}
