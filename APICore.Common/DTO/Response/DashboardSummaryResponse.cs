namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// KPIs del dashboard principal.
    /// </summary>
    public class DashboardSummaryResponse
    {
        public int TotalProducts { get; set; }
        public decimal TotalProductsTrend { get; set; }
        public decimal InventoryValue { get; set; }
        public decimal InventoryValueTrend { get; set; }
        public int LowStockCount { get; set; }
        public int LowStockChange { get; set; }
        public int WeeklyOrders { get; set; }
        public decimal WeeklyOrdersTrend { get; set; }
    }
}
