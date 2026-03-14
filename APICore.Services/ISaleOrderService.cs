using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data.Entities;
using APICore.Services.Utils;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface ISaleOrderService
    {
        Task<SaleOrder> CreateSaleOrder(CreateSaleOrderRequest request, int userId);
        Task<SaleOrder> ConfirmSaleOrder(int id, int userId);
        Task<SaleOrder> CancelSaleOrder(int id, int userId);
        Task<SaleOrder> UpdateSaleOrder(int id, UpdateSaleOrderRequest request);
        Task<SaleOrder> GetSaleOrder(int id);
        Task<PaginatedList<SaleOrder>> GetAllSaleOrders(int? page, int? perPage, string? status, string? sortOrder);
        Task<SaleStatsResponse> GetStats(int? days);
    }
}
