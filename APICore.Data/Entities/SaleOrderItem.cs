using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class SaleOrderItem : BaseEntity
    {
        [Required]
        public int SaleOrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public decimal Quantity { get; set; }

        /// <summary>
        /// Precio unitario al momento de la venta (snapshot, no cambia con el producto).
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Precio original del producto antes de aplicar promoción.
        /// </summary>
        public decimal OriginalUnitPrice { get; set; }

        /// <summary>
        /// Costo unitario al momento de la venta (snapshot). Necesario para calcular margen.
        /// </summary>
        public decimal UnitCost { get; set; }

        public int? PromotionId { get; set; }

        public decimal Discount { get; set; }

        /// <summary>
        /// Subtotal de la línea: (Quantity * UnitPrice) - Discount.
        /// </summary>
        public decimal LineTotal { get; set; }

        public SaleOrder? SaleOrder { get; set; }
        public Product? Product { get; set; }
        public Promotion? Promotion { get; set; }
    }
}
