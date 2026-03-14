namespace APICore.Common.DTO.Response
{
    public class SaleStatsResponse
    {
        public int TotalSales { get; set; }
        public int SalesToday { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCogs { get; set; }
        public decimal GrossMargin { get; set; }
        public decimal GrossMarginPercent { get; set; }
        public int TotalReturns { get; set; }
        public decimal TotalReturnsAmount { get; set; }
    }
}
