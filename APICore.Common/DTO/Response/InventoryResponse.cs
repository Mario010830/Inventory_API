using System;

namespace APICore.Common.DTO.Response
{
    public class InventoryResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal MinimumStock { get; set; }
        public string UnitOfMeasure { get; set; }
        public int LocationId { get; set; }
        public string? LocationName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}
