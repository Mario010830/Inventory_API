using System;
using System.Threading.Tasks;
using APICore.Common.DTO.Response.Reports;

namespace APICore.Services
{
    public interface IReportsService
    {
        Task<SalesReportResponse> GetSalesReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId, int page = 1, int pageSize = 50);
        Task<byte[]> ExportSalesOrdersCsvAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId);
        Task<byte[]> ExportSalesOrdersPdfAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId);
        Task<InventoryReportResponse> GetInventoryReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId);
        Task<ProductsReportResponse> GetProductsReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId);
        Task<CrmReportResponse> GetCrmReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId);
        Task<OperationsReportResponse> GetOperationsReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId);

        Task<SalesSummaryReportResponse> GetSalesSummaryReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId);
        Task<SalesByProductReportResponse> GetSalesByProductReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId, int page = 1, int pageSize = 50);
        Task<SalesByCategoryReportResponse> GetSalesByCategoryReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId);
        Task<SalesByEmployeeReportResponse> GetSalesByEmployeeReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId);
        Task<SalesByPaymentReportResponse> GetSalesByPaymentReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId);
        Task<ReceiptsReportResponse> GetReceiptsReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId, string? folioContains, int page = 1, int pageSize = 50);
        Task<SalesByModifierReportResponse> GetSalesByModifierReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId);
        Task<SalesDiscountsReportResponse> GetSalesDiscountsReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId);
        Task<SalesTaxesReportResponse> GetSalesTaxesReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId);
        Task<CashRegisterReportResponse> GetCashRegisterReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId);
    }
}

