using System;

namespace APICore.Common.DTO.Response
{
    public class InventoryMovementResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int LocationId { get; set; }
        public string? LocationName { get; set; }
        public string Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal? PreviousStock { get; set; }
        public decimal? NewStock { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Reason { get; set; }
        public int? SupplierId { get; set; }
        public string? ReferenceDocument { get; set; }
        public int? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
