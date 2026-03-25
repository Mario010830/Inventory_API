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
    }
}

