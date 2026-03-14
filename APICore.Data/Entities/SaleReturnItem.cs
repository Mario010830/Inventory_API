using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class SaleReturnItem : BaseEntity
    {
        [Required]
        public int SaleReturnId { get; set; }

        [Required]
        public int SaleOrderItemId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public decimal Quantity { get; set; }

        /// <summary>
        /// Precio unitario heredado del ítem original de venta.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Total de la línea devuelta: Quantity * UnitPrice.
        /// </summary>
        public decimal LineTotal { get; set; }

        public SaleReturn? SaleReturn { get; set; }
        public SaleOrderItem? SaleOrderItem { get; set; }
        public Product? Product { get; set; }
    }
}
