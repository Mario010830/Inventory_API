#nullable enable

namespace APICore.Common.DTO.Request
{
    public class UpdateInventoryRequest
    {
        public int? ProductId { get; set; }
        public decimal? CurrentStock { get; set; }
        public decimal? MinimumStock { get; set; }
        public string? UnitOfMeasure { get; set; }
        public int? LocationId { get; set; }
    }
}
