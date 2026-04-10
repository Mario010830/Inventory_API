namespace APICore.Common.DTO.Response
{
    public class DailySummaryInventoryItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public decimal StockBefore { get; set; }
        public decimal StockAfter { get; set; }
        public decimal StockDifference { get; set; }
    }
}
