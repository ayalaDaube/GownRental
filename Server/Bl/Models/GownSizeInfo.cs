namespace Bl.Models
{
    /// <summary>One size of a gown model as shown to the client.</summary>
    public class GownSizeInfo
    {
        public int GownId { get; set; }

        public int ModelId { get; set; }

        public int Size { get; set; }

        public double Price { get; set; }

        /// <summary>The gown is part of the inventory (not retired or damaged).</summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// The gown can be rented. Without a date this equals IsActive; for a chosen date it is also
        /// false when the gown is rented within the buffer days around that date.
        /// </summary>
        public bool IsAvailable { get; set; }
    }
}
