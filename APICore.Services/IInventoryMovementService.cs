using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using APICore.Services.Utils;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IInventoryMovementService
    {
        /// <param name="userLocationId">Si el usuario tiene ubicación asignada, se usa esta y se ignora request.LocationId (el movimiento queda fijo en su ubicación).</param>
        Task<InventoryMovement> CreateMovement(CreateInventoryMovementRequest request, int userId, int? userLocationId = null);
        Task<InventoryMovement> GetMovement(int id);
        Task<PaginatedList<InventoryMovement>> GetAllMovements(int? page, int? perPage, string sortOrder = null);
        Task<PaginatedList<InventoryMovement>> GetMovementsByProduct(int productId, int locationId, int? page, int? perPage);
    }
}
