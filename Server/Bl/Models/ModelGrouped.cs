namespace Bl.Models
{
    /// <summary>A gown model with all of its sizes.</summary>
    public class ModelGrouped
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string? Image { get; set; }
        public string? Description { get; set; }
        public List<GownSizeInfo> Details { get; set; } = new();
    }
}
