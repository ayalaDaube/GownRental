using System;
using System.Collections.Generic;

namespace Dal.Models;

public partial class Appointment
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    // CustomerId can be null in the DB; make it nullable to match DB state
    public string? CustomerId { get; set; }
    public TimeOnly Hour { get; set; }
    public int? NumberOfPeople { get; set; }
    public bool? IsAvailable { get; set; }

    // Navigation property should be nullable when FK can be null
    public virtual Customer? Customer { get; set; }
}
