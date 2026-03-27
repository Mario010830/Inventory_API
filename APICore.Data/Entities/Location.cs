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
        public string? WhatsAppContact { get; set; }
      
        public string? PhotoUrl { get; set; }
       
        public string? Province { get; set; }
        
        public string? Municipality { get; set; }
        
        public string? Street { get; set; }
        
        public string? BusinessHoursJson { get; set; }
       
        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public bool IsVerified { get; set; }
        public bool OffersDelivery { get; set; } = true;
        public bool OffersPickup { get; set; } = true;
        public string? DeliveryHoursJson { get; set; }
        public string? PickupHoursJson { get; set; }

        public int? BusinessCategoryId { get; set; }
        public virtual BusinessCategory? BusinessCategory { get; set; }

        public virtual Organization Organization { get; set; } = null!;
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public virtual ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
    }
}
