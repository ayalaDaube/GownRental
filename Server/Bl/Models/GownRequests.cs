using System.ComponentModel.DataAnnotations;

namespace Bl.Models
{
    /// <summary>
    /// Adds a gown. Without ModelId a new model is created (name and colour are then required);
    /// with ModelId a new size is added to that existing model. Ids are always assigned by the server.
    /// </summary>
    public class NewGownRequest
    {
        public int? ModelId { get; set; }

        [StringLength(60)]
        public string? Name { get; set; }

        [StringLength(30)]
        public string? Color { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        [Url, StringLength(500)]
        public string? ImageUrl { get; set; }

        [Range(1, 100)]
        public int Size { get; set; }

        [Range(1, 100000)]
        public double Price { get; set; }
    }

    /// <summary>The editable fields of one size of a gown.</summary>
    public class UpdateSizeRequest
    {
        [Range(1, 100)]
        public int Size { get; set; }

        [Range(1, 100000)]
        public double Price { get; set; }

        public bool IsAvailable { get; set; } = true;
    }

    /// <summary>The editable fields shared by all sizes of a model.</summary>
    public class UpdateModelRequest
    {
        [Required, StringLength(60, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(30, MinimumLength = 1)]
        public string Color { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        [Url, StringLength(500)]
        public string? ImageUrl { get; set; }
    }
}
