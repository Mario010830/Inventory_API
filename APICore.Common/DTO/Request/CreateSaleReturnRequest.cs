using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class CreateSaleReturnRequest
    {
        [Required]
        public int SaleOrderId { get; set; }

        public string? Reason { get; set; }
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
