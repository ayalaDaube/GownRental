using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bl.Models
{
    public class ModelFullDetaills
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        /// <summary>True while the gown is part of the inventory.</summary>
        public bool IsActive { get; set; }
        public int ModelId { get; set; }
        public double Price { get; set; }
        public int Size { get; set; }

    }
}
