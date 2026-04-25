using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// Resumen público de un pedido para enlaces compartidos (sin datos internos).
    /// </summary>
    public class PublicOrderResponse
    {
        public int Id { get; set; }
        public string? Folio { get; set; }
        public string Status { get; set; } = null!;
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int LocationId { get; set; }
        public string? LocationName { get; set; }
        public List<PublicOrderItemResponse> Items { get; set; } = new();
    }

    public class PublicOrderItemResponse
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal LineTotal { get; set; }
    }
}
