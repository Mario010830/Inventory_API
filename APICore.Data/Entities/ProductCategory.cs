using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class ProductCategory: BaseEntity
    {
        #nullable enable
        public int OrganizationId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Color { get; set; } = "#6366f1";
        public string Icon { get; set; } = "category";
        public Organization? Organization { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
