using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class Supplier: BaseEntity
    {
        public int OrganizationId { get; set; }
        public string Name { get; set; } = null!;
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
        public Organization? Organization { get; set; }
        public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
    }
}
