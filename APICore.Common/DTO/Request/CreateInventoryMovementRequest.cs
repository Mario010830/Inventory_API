namespace APICore.Common.DTO.Request
{
    public class CreateInventoryMovementRequest
    {
        public int ProductId { get; set; }
        public int LocationId { get; set; }
        public int Type { get; set; }
        public decimal Quantity { get; set; }
        public string? Reason { get; set; }
        public int? SupplierId { get; set; }
        public string? ReferenceDocument { get; set; }
    }
}
