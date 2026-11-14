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
    }
}
