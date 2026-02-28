using System.Collections.Generic;

namespace APICore.Data.Entities
{
    /// <summary>
    /// Localización (almacén o punto) perteneciente a una organización.
    /// </summary>
    public class Location : BaseEntity
    {
        public int OrganizationId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }

        public virtual Organization Organization { get; set; } = null!;
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
    }
}
