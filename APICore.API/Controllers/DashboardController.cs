using APICore.API.BasicResponses;
using APICore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Authorize]
    [Route("api/dashboard")]
    public class DashboardController : Controller
    {
        private readonly IDashboardStatsService _dashboardStatsService;

        public DashboardController(IDashboardStatsService dashboardStatsService)
        {
            _dashboardStatsService = dashboardStatsService ?? throw new ArgumentNullException(nameof(dashboardStatsService));
        }

        /// <summary>
        /// KPIs resumen del dashboard principal.
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _dashboardStatsService.GetDashboardSummaryAsync(from, to);
            return Ok(new ApiOkResponse(result));
        }

        /// <summary>
        /// Flujo de inventario (gráfico de línea) por día.
        /// </summary>
        [HttpGet("inventory-flow")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetInventoryFlow(
            [FromQuery] int days = 7,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            if (days < 1 || days > 90) days = 7;
            var result = await _dashboardStatsService.GetInventoryFlowAsync(days, from, to);
            return Ok(new ApiOkResponse(result));
        }

        /// <summary>
        /// Distribución por categoría (gráfico dona).
        /// </summary>
        [HttpGet("category-distribution")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCategoryDistribution()
        {
            var result = await _dashboardStatsService.GetCategoryDistributionAsync();
            return Ok(new ApiOkResponse(result));
        }

        /// <summary>
        /// Evolución del valor del inventario por mes (gráfico de línea).
        /// </summary>
        [HttpGet("inventory-value-evolution")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetInventoryValueEvolution(
            [FromQuery] int months = 6,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            if (months < 1 || months > 24) months = 6;
            var result = await _dashboardStatsService.GetInventoryValueEvolutionAsync(months, from, to);
            return Ok(new ApiOkResponse(result));
        }

        /// <summary>
        /// Estado del stock: En rango / Bajo / Crítico (gráfico dona).
        /// </summary>
        [HttpGet("stock-status")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStockStatus()
        {
            var result = await _dashboardStatsService.GetStockStatusAsync();
            return Ok(new ApiOkResponse(result));
        }

        /// <summary>
        /// Productos con más movimientos (lista para tarjeta "Ver más").
        /// </summary>
        [HttpGet("list-top-movements")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetListTopMovements([FromQuery] int days = 30, [FromQuery] int limit = 5)
        {
            if (limit < 1 || limit > 50) limit = 5;
            if (days < 1 || days > 365) days = 30;
            var result = await _dashboardStatsService.GetListTopMovementsAsync(days, limit);
            return Ok(new ApiOkResponse(result));
        }

        /// <summary>
        /// Productos con stock bajo (lista para tarjeta "Ver más").
        /// </summary>
        [HttpGet("list-low-stock")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetListLowStock([FromQuery] int limit = 5)
        {
            if (limit < 1 || limit > 50) limit = 5;
            var result = await _dashboardStatsService.GetListLowStockAsync(limit);
            return Ok(new ApiOkResponse(result));
        }

        /// <summary>
        /// Últimos movimientos (lista para tarjeta "Ver más").
        /// </summary>
        [HttpGet("list-latest-movements")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetListLatestMovements([FromQuery] int limit = 5)
        {
            if (limit < 1 || limit > 50) limit = 5;
            var result = await _dashboardStatsService.GetListLatestMovementsAsync(limit);
            return Ok(new ApiOkResponse(result));
        }

        /// <summary>
        /// Valor por ubicación (lista para tarjeta "Ver más").
        /// </summary>
        [HttpGet("list-value-by-location")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetListValueByLocation([FromQuery] int limit = 5)
        {
            if (limit < 1 || limit > 50) limit = 5;
            var result = await _dashboardStatsService.GetListValueByLocationAsync(limit);
            return Ok(new ApiOkResponse(result));
        }

        /// <summary>
        /// Productos añadidos recientemente (lista para tarjeta "Ver más").
        /// </summary>
        [HttpGet("list-recent-products")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetListRecentProducts([FromQuery] int limit = 5, [FromQuery] int days = 30)
        {
            if (limit < 1 || limit > 50) limit = 5;
            if (days < 1 || days > 365) days = 30;
            var result = await _dashboardStatsService.GetListRecentProductsAsync(limit, days);
            return Ok(new ApiOkResponse(result));
        }

        /// <summary>
        /// Entradas vs salidas por día (gráfico compuesto: barras + línea).
        /// </summary>
        [HttpGet("entries-vs-exits")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetEntriesVsExits(
            [FromQuery] int days = 7,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            if (days < 1 || days > 90) days = 7;
            var result = await _dashboardStatsService.GetEntriesVsExitsAsync(days, from, to);
            return Ok(new ApiOkResponse(result));
        }

        /// <summary>
        /// Alertas de stock bajo por día (gráfico de línea/área).
        /// </summary>
        [HttpGet("low-stock-alerts-by-day")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLowStockAlertsByDay([FromQuery] int days = 7)
        {
            if (days < 1 || days > 90) days = 7;
            var result = await _dashboardStatsService.GetLowStockAlertsByDayAsync(days);
            return Ok(new ApiOkResponse(result));
        }
    }
}
