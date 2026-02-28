using System.Collections.Generic;

namespace APICore.Data.Entities
{

    public class Organization : BaseEntity
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }

        public virtual ICollection<Location> Locations { get; set; } = new List<Location>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
        public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
        public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}
