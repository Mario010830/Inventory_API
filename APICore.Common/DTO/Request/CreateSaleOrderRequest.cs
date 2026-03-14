using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class CreateSaleOrderRequest
    {
        [Required]
        public int LocationId { get; set; }

        /// <summary>
        /// Cliente asociado. Opcional.
        /// </summary>
        public int? ContactId { get; set; }

        public string? Notes { get; set; }

        public decimal DiscountAmount { get; set; } = 0;

        [Required]
        [MinLength(1)]
        public List<CreateSaleOrderItemRequest> Items { get; set; } = new();
    }

    public class CreateSaleOrderItemRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(0.001, double.MaxValue)]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Si se omite, se toma el precio actual del producto.
        /// </summary>
        public decimal? UnitPrice { get; set; }

        public decimal Discount { get; set; } = 0;
    }
}
