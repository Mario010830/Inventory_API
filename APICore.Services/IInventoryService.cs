using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data.Entities;
using APICore.Services.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IInventoryService
    {
        Task<Inventory> CreateInventory(CreateInventoryRequest request);
        Task DeleteInventory(int id);
        Task UpdateInventory(int id, UpdateInventoryRequest request);
        Task<Inventory> GetInventory(int id);
        Task<PaginatedList<Inventory>> GetAllInventories(int? page, int? perPage, string sortOrder = null);
        Task<IEnumerable<ProductStockByLocationResponse>> GetStockByProductForLocation(int locationId);
    }
}
