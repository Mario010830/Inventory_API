namespace APICore.Common.DTO.Request
{
    public class CreateInventoryRequest
    {
        public int ProductId { get; set; }
        public int LocationId { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal MinimumStock { get; set; }
        public string UnitOfMeasure { get; set; } = "unit";
    }
}
