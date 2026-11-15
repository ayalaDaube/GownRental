namespace Dal.Models;

/// <summary>A rental together with its gown line, read from the database in one query.</summary>
public record RentalWithDetails(Rental Rental, RentalDetails Details);
