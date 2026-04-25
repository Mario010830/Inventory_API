using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// Cuerpo mínimo del POST crear orden (201) para que el cliente pueda enlazar /pedido/{id} sin cargar el detalle completo.
    /// </summary>
    public class SaleOrderCreatedResponse
    {
        public int Id { get; set; }
        public string? Folio { get; set; }
    }

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
        public string? PaymentMethodInstrumentReference { get; set; }

        /// <summary>Aporte en CUP.</summary>
        public decimal Amount { get; set; }

        public string? Reference { get; set; }
        public int? CurrencyId { get; set; }
        public string? CurrencyCode { get; set; }
        public decimal? AmountForeign { get; set; }
        public decimal? ExchangeRateSnapshot { get; set; }
        public List<SaleOrderPaymentDenominationResponse> Denominations { get; set; } = new();
    }

    public class SaleOrderPaymentDenominationResponse
    {
        public string Kind { get; set; } = null!;
        public decimal Value { get; set; }
        public int Quantity { get; set; }
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
