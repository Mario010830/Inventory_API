using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using APICore.Services.Utils;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface ISaleReturnService
    {
        Task<SaleReturn> CreateSaleReturn(CreateSaleReturnRequest request, int userId);
        Task<SaleReturn> GetSaleReturn(int id);
        Task<PaginatedList<SaleReturn>> GetAllSaleReturns(int? page, int? perPage, string? sortOrder);
        Task<PaginatedList<SaleReturn>> GetReturnsBySaleOrder(int saleOrderId, int? page, int? perPage);
    }
}
