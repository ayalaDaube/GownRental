using Bl.Exceptions;

namespace Bl
{
    /// <summary>Business rules about renting that more than one service needs.</summary>
    public static class RentalRules
    {
        /// <summary>
        /// A rented gown is unavailable this many days before and after its event
        /// (time for delivery, cleaning and repairs).
        /// </summary>
        public const int BufferDays = 3;

        /// <summary>Events are planned at most this many years ahead.</summary>
        public const int MaxYearsAhead = 2;

        /// <summary>
        /// Refuses a date that is too far ahead. This also keeps every later date calculation
        /// (the buffer days around an event) away from the end of the calendar.
        /// </summary>
        public static void EnsureNotTooFarAhead(DateOnly date, DateOnly today)
        {
            if (date > today.AddYears(MaxYearsAhead))
            {
                throw new InvalidRequestException(
                    $"The event date cannot be more than {MaxYearsAhead} years ahead.");
            }
        }
    }
}
