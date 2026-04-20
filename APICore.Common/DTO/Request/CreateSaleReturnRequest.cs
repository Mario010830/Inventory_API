using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class CreateSaleReturnRequest
    {
        [Required]
        public int SaleOrderId { get; set; }

        /// <summary>Motivo libre (ej. error de cobro, cambio de opinión, producto defectuoso).</summary>
        public string? Reason { get; set; }

        /// <summary>Notas adicionales. Una devolución no implica necesariamente producto defectuoso.</summary>
        public string? Notes { get; set; }

        [Required]
        [MinLength(1)]
        public List<CreateSaleReturnItemRequest> Items { get; set; } = new();
    }

    public class CreateSaleReturnItemRequest
    {
        [Required]
        public int SaleOrderItemId { get; set; }

        [Required]
        [Range(0.001, double.MaxValue)]
        public decimal Quantity { get; set; }
    }
}
