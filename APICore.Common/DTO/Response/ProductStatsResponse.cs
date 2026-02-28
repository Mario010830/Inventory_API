namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// KPIs de la vista de productos.
    /// </summary>
    public class ProductStatsResponse
    {
        public int TotalProducts { get; set; }
        public decimal TotalProductsTrend { get; set; }
        public decimal InventoryValue { get; set; }
        public decimal InventoryValueTrend { get; set; }
        public int CriticalStockCount { get; set; }
        public int CriticalStockTrend { get; set; }
        public int MovementsToday { get; set; }
        public decimal MovementsTodayTrend { get; set; }
    }
}
