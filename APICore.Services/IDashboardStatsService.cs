using APICore.Common.DTO.Response;
using System;
using System.Threading.Tasks;

namespace APICore.Services
{
    /// <summary>
    /// Servicio de estadísticas para el dashboard y vistas de productos, categorías, proveedores, inventario y movimientos.
    /// </summary>
    public interface IDashboardStatsService
    {
        // Dashboard principal
        Task<DashboardSummaryResponse> GetDashboardSummaryAsync(DateTime? from, DateTime? to);
        Task<ChartLineResponse> GetInventoryFlowAsync(int days, DateTime? from, DateTime? to);
        Task<ChartDonutResponse> GetCategoryDistributionAsync();
        Task<ChartLineResponse> GetInventoryValueEvolutionAsync(int months, DateTime? from, DateTime? to);
        Task<ChartDonutResponse> GetStockStatusAsync();
        Task<ListCardResponse> GetListTopMovementsAsync(int days, int limit);
        Task<ListCardResponse> GetListLowStockAsync(int limit);
        Task<ListCardResponse> GetListLatestMovementsAsync(int limit);
        Task<ListCardResponse> GetListValueByLocationAsync(int limit);
        Task<ListCardResponse> GetListRecentProductsAsync(int limit, int days);
        Task<ChartComposedResponse> GetEntriesVsExitsAsync(int days, DateTime? from, DateTime? to);
        Task<ChartLineResponse> GetLowStockAlertsByDayAsync(int days);

        // Productos
        Task<ProductStatsResponse> GetProductStatsAsync(DateTime? from, DateTime? to);
        Task<ChartLineResponse> GetProductPerformanceAsync(int days, DateTime? from, DateTime? to);
        Task<ChartDonutResponse> GetStockByCategoryAsync();

        // Categorías
        Task<CategoryStatsResponse> GetCategoryStatsAsync();
        Task<ChartLineResponse> GetCategoryItemDistributionAsync(string? period, int? days);
        Task<ChartDonutResponse> GetCategoryStorageUsageAsync();

        // Proveedores
        Task<SupplierStatsResponse> GetSupplierStatsAsync(DateTime? from, DateTime? to);
        Task<ChartLineResponse> GetSupplierDeliveryFrequencyAsync(int? days, DateTime? from, DateTime? to);
        Task<ChartLineResponse> GetSupplierDeliveryTimelineAsync(int? days, DateTime? from, DateTime? to);
        Task<ChartDonutResponse> GetSupplierCategoryDistributionAsync();

        // Inventario
        Task<InventoryStatsResponse> GetInventoryStatsAsync();
        Task<ChartLineResponse> GetInventoryFlowAsync(int days);
        Task<ChartLineResponse> GetStockByLocationAsync();
        Task<ChartDonutResponse> GetInventoryCategoryDistributionAsync();

        // Movimientos
        Task<MovementStatsResponse> GetMovementStatsAsync(DateTime? from, DateTime? to, bool todayOnly);
        Task<ChartLineResponse> GetMovementFlowAsync(int days);
        Task<ChartComposedResponse> GetMovementFlowWithCumulativeAsync(int days);
        Task<ChartDonutResponse> GetMovementDistributionByTypeAsync();
    }
}
