using APICore.Data.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class InventoryMovement : BaseEntity
    {
        [Required]
        public int ProductId { get; set; }

        /// <summary>
        /// Almacén donde se realiza el movimiento. Requerido para multitenancy.
        /// </summary>
        [Required]
        public int LocationId { get; set; }

        [Required]
        public InventoryMovementType Type { get; set; }
        public InventoryMovementReason? Reason { get; set; }
        public decimal Quantity { get; set; }
        public decimal? PreviousStock { get; set; }
        public decimal? NewStock { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? UnitPrice { get; set; }
        public int? SupplierId { get; set; }
        public string? ReferenceDocument { get; set; }
        public int? UserId { get; set; }
        public int? SaleOrderId { get; set; }
        public Product Product { get; set; } = null!;
        public Location Location { get; set; } = null!;
        public Supplier? Supplier { get; set; }
        public SaleOrder? SaleOrder { get; set; }
    }
}