using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    public class SaleReturnResponse
    {
        public int Id { get; set; }
        public int OrganizationId { get; set; }
        public int SaleOrderId { get; set; }
        public string? SaleOrderFolio { get; set; }
        public int LocationId { get; set; }
        public string? LocationName { get; set; }
        public string Status { get; set; } = null!;
        public string? Reason { get; set; }
        public string? Notes { get; set; }
        public decimal Total { get; set; }
        public int? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public List<SaleReturnItemResponse> Items { get; set; } = new();
    }

    public class SaleReturnItemResponse
    {
        public int Id { get; set; }
        public int SaleReturnId { get; set; }
        public int SaleOrderItemId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
