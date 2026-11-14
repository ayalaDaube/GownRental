using Dal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bl.Models
{
    public class ModelGrouped
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public string? Image { get; set; }
        public string Description { get; set; }
        public List<ModelDetails> Details { get; set; }
    }
}
