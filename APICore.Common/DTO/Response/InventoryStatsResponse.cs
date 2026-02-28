namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// KPIs de la vista de inventario.
    /// </summary>
    public class InventoryStatsResponse
    {
        public decimal TotalValue { get; set; }
        public decimal TotalValueTrend { get; set; }
        public int LowStockCount { get; set; }
        public int LowStockNewToday { get; set; }
        public int MovementsCount { get; set; }
        public decimal MovementsTrend { get; set; }
        public int ProductsCount { get; set; }
        public int ProductsNewCount { get; set; }
    }
}
