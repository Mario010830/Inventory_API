using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.Common.Constants;
using APICore.Common.DTO.Response;
using APICore.Common.DTO.Response.Reports;
using APICore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
 

namespace APICore.API.Controllers
{
    [Authorize]
    [Route("api/reports")]
    public class ReportsController : Controller
    {
        private readonly IReportsService _reportsService;

        public ReportsController(IReportsService reportsService)
        {
            _reportsService = reportsService ?? throw new ArgumentNullException(nameof(reportsService));
        }

        [HttpGet("sales/export/pdf")]
        [RequirePermission(PermissionCodes.SaleReport)]
        public async Task<IActionResult> ExportSalesOrdersPdf(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null)
        {
            var bytes = await _reportsService.ExportSalesOrdersPdfAsync(dateFrom, dateTo, locationId);
            var fileName = $"reporte-ventas-pedidos-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        [HttpGet("sales/export")]
        [RequirePermission(PermissionCodes.SaleReport)]
        public async Task<IActionResult> ExportSalesOrdersCsv(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null)
        {
            var bytes = await _reportsService.ExportSalesOrdersCsvAsync(dateFrom, dateTo, locationId);
            var fileName = $"reporte-ventas-pedidos-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        [HttpGet("sales")]
        [RequirePermission(PermissionCodes.SaleReport)]
        public async Task<IActionResult> GetSalesReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var response = await _reportsService.GetSalesReportAsync(dateFrom, dateTo, locationId, page, pageSize);
            return Ok(new ApiOkResponse(response));
        }

        [HttpGet("inventory")]
        [RequirePermission(PermissionCodes.InventoryRead)]
        public async Task<IActionResult> GetInventoryReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null)
        {
            var response = await _reportsService.GetInventoryReportAsync(dateFrom, dateTo, locationId);
            return Ok(new ApiOkResponse(response));
        }

        [HttpGet("products")]
        [RequirePermission(PermissionCodes.ProductRead)]
        public async Task<IActionResult> GetProductsReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null)
        {
            var response = await _reportsService.GetProductsReportAsync(dateFrom, dateTo, locationId);
            return Ok(new ApiOkResponse(response));
        }

        [HttpGet("crm")]
        [RequirePermission(PermissionCodes.LeadRead)]
        public async Task<IActionResult> GetCrmReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null)
        {
            var response = await _reportsService.GetCrmReportAsync(dateFrom, dateTo, locationId);
            return Ok(new ApiOkResponse(response));
        }

        [HttpGet("operations")]
        [RequirePermission(PermissionCodes.InventoryMovementRead)]
        public async Task<IActionResult> GetOperationsReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null)
        {
            var response = await _reportsService.GetOperationsReportAsync(dateFrom, dateTo, locationId);
            return Ok(new ApiOkResponse(response));
        }
    }
}

