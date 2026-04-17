using APICore.Data.Entities.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class SaleOrder : BaseEntity
    {
        [Required]
        public int OrganizationId { get; set; }

        [Required]
        public int LocationId { get; set; }

        /// <summary>
        /// Cliente asociado a la venta. Opcional.
        /// </summary>
        public int? ContactId { get; set; }

        public SaleOrderStatus Status { get; set; } = SaleOrderStatus.draft;

        public string? Notes { get; set; }

        /// <summary>
        /// Suma de (Quantity * UnitPrice) de todos los ítems antes de descuentos.
        /// </summary>
        public decimal Subtotal { get; set; }

        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// Total final = Subtotal - DiscountAmount.
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Número de folio/referencia legible, ej: VENTA-0001.
        /// </summary>
        public string? Folio { get; set; }

        public int? UserId { get; set; }

        public Organization? Organization { get; set; }
        public Location? Location { get; set; }
        public Contact? Contact { get; set; }

        public ICollection<SaleOrderItem> Items { get; set; } = new List<SaleOrderItem>();
        public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
        public ICollection<SaleReturn> Returns { get; set; } = new List<SaleReturn>();
        public ICollection<SaleOrderPayment> Payments { get; set; } = new List<SaleOrderPayment>();
    }
}
