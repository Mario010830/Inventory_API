using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    public class SaleOrderResponse
    {
        public int Id { get; set; }
        public string? Folio { get; set; }
        public int OrganizationId { get; set; }
        public int LocationId { get; set; }
        public string? LocationName { get; set; }
        public int? ContactId { get; set; }
        public string? ContactName { get; set; }
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public int? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public List<SaleOrderItemResponse> Items { get; set; } = new();
        public List<SaleOrderPaymentResponse> Payments { get; set; } = new();
    }

    public class SaleOrderPaymentResponse
    {
        public int Id { get; set; }
        public int SaleOrderId { get; set; }
        public int PaymentMethodId { get; set; }
        public string? PaymentMethodName { get; set; }
        public decimal Amount { get; set; }
    }

    public class SaleOrderItemResponse
    {
        public int Id { get; set; }
        public int SaleOrderId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal OriginalUnitPrice { get; set; }
        public decimal UnitCost { get; set; }
        public int? PromotionId { get; set; }
        public decimal Discount { get; set; }
        public decimal LineTotal { get; set; }

        /// <summary>
        /// Margen bruto de la línea: (UnitPrice - UnitCost) * Quantity - Discount.
        /// </summary>
        public decimal GrossMargin { get; set; }
    }
}
