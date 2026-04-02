using System.Collections.Generic;

namespace APICore.Data.Entities
{

    public class Organization : BaseEntity
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Verificación a nivel plataforma. Al cambiar, se sincroniza <see cref="Location.IsVerified"/> en todas las ubicaciones de la organización.
        /// </summary>
        public bool IsVerified { get; set; }

        public int? SubscriptionId { get; set; }
        public virtual Subscription? Subscription { get; set; }

        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

        public virtual ICollection<Location> Locations { get; set; } = new List<Location>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
        public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
        public virtual ICollection<Currency> Currencies { get; set; } = new List<Currency>();
        public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}
