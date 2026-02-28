using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using APICore.Services.Utils;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IInventoryMovementService
    {
        Task<InventoryMovement> CreateMovement(CreateInventoryMovementRequest request, int userId);
        Task<InventoryMovement> GetMovement(int id);
        Task<PaginatedList<InventoryMovement>> GetAllMovements(int? page, int? perPage, string sortOrder = null);
        Task<PaginatedList<InventoryMovement>> GetMovementsByProduct(int productId, int locationId, int? page, int? perPage);
    }
}
