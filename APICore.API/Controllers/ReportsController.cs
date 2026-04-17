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

        /// <summary>Resumen de ventas (totales y series diarias, sin listado de tickets).</summary>
        [HttpGet("sales/summary")]
        [RequirePermission(PermissionCodes.SaleReport)]
        public async Task<IActionResult> GetSalesSummaryReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null)
        {
            var response = await _reportsService.GetSalesSummaryReportAsync(dateFrom, dateTo, locationId);
            return Ok(new ApiOkResponse(response));
        }

        /// <summary>Ventas por artículo (líneas confirmadas, paginado).</summary>
        [HttpGet("sales/by-product")]
        [RequirePermission(PermissionCodes.SaleReport)]
        public async Task<IActionResult> GetSalesByProductReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var response = await _reportsService.GetSalesByProductReportAsync(dateFrom, dateTo, locationId, page, pageSize);
            return Ok(new ApiOkResponse(response));
        }

        /// <summary>Ventas por categoría de producto.</summary>
        [HttpGet("sales/by-category")]
        [RequirePermission(PermissionCodes.SaleReport)]
        public async Task<IActionResult> GetSalesByCategoryReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null)
        {
            var response = await _reportsService.GetSalesByCategoryReportAsync(dateFrom, dateTo, locationId);
            return Ok(new ApiOkResponse(response));
        }

        /// <summary>Ventas por empleado (usuario que confirma la venta).</summary>
        [HttpGet("sales/by-employee")]
        [RequirePermission(PermissionCodes.SaleReport)]
        public async Task<IActionResult> GetSalesByEmployeeReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null)
        {
            var response = await _reportsService.GetSalesByEmployeeReportAsync(dateFrom, dateTo, locationId);
            return Ok(new ApiOkResponse(response));
        }

        /// <summary>Ventas por método de pago (incluye referencia de instrumento predefinida).</summary>
        [HttpGet("sales/by-payment")]
        [RequirePermission(PermissionCodes.SaleReport)]
        public async Task<IActionResult> GetSalesByPaymentReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null)
        {
            var response = await _reportsService.GetSalesByPaymentReportAsync(dateFrom, dateTo, locationId);
            return Ok(new ApiOkResponse(response));
        }

        /// <summary>Recibos (ventas confirmadas, paginado; búsqueda opcional por folio).</summary>
        [HttpGet("sales/receipts")]
        [RequirePermission(PermissionCodes.SaleReport)]
        public async Task<IActionResult> GetReceiptsReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null,
            [FromQuery] string? folioContains = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var response = await _reportsService.GetReceiptsReportAsync(dateFrom, dateTo, locationId, folioContains, page, pageSize);
            return Ok(new ApiOkResponse(response));
        }

        /// <summary>Placeholder: modificadores de artículo no existen en el modelo actual.</summary>
        [HttpGet("sales/by-modifier")]
        [RequirePermission(PermissionCodes.SaleReport)]
        public async Task<IActionResult> GetSalesByModifierReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null)
        {
            var response = await _reportsService.GetSalesByModifierReportAsync(dateFrom, dateTo, locationId);
            return Ok(new ApiOkResponse(response));
        }

        /// <summary>Descuentos en cabecera, en líneas y por promoción.</summary>
        [HttpGet("sales/discounts")]
        [RequirePermission(PermissionCodes.SaleReport)]
        public async Task<IActionResult> GetSalesDiscountsReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null)
        {
            var response = await _reportsService.GetSalesDiscountsReportAsync(dateFrom, dateTo, locationId);
            return Ok(new ApiOkResponse(response));
        }

        /// <summary>Placeholder: impuestos no modelados en ventas.</summary>
        [HttpGet("sales/taxes")]
        [RequirePermission(PermissionCodes.SaleReport)]
        public async Task<IActionResult> GetSalesTaxesReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null)
        {
            var response = await _reportsService.GetSalesTaxesReportAsync(dateFrom, dateTo, locationId);
            return Ok(new ApiOkResponse(response));
        }

        /// <summary>Caja: retiros registrados y desglose por método de pago en el periodo.</summary>
        [HttpGet("sales/cash-register")]
        [HttpGet("sales/caja")]
        [RequirePermission(PermissionCodes.SaleReport)]
        public async Task<IActionResult> GetCashRegisterReport(
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? locationId = null)
        {
            var response = await _reportsService.GetCashRegisterReportAsync(dateFrom, dateTo, locationId);
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

