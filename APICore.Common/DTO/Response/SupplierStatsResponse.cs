namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// KPIs de la vista de proveedores.
    /// </summary>
    public class SupplierStatsResponse
    {
        public int TotalSuppliers { get; set; }
        public decimal TotalSuppliersTrend { get; set; }
        public int ActiveOrders { get; set; }
        public decimal ActiveOrdersTrend { get; set; }
        public decimal CompliancePercent { get; set; }
        public decimal ComplianceTrend { get; set; }
        public decimal MonthlyExpenses { get; set; }
        public decimal MonthlyExpensesTrend { get; set; }
    }
}
