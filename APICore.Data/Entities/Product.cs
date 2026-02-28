using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APICore.Data.Entities
{
    public class Product : BaseEntity
    {
        public int OrganizationId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public decimal Precio { get; set; } = 0;
        public decimal Costo { get; set; } = 0;
        public string ImagenUrl { get; set; }
        public bool IsAvailable { get; set; }

        public ProductCategory? Category { get; set; }
        public Organization? Organization { get; set; }

        /// <summary>
        /// Un producto puede tener varios inventarios (uno por almacén).
        /// </summary>
        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

        public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
    }
}
