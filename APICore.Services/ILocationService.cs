using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data.Entities;
using APICore.Services.Utils;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface ILocationService
    {
        Task<LocationResponse> CreateLocation(CreateLocationRequest request);
        Task DeleteLocation(int id);
        Task<LocationResponse> GetLocation(int id);
        Task<PaginatedList<LocationResponse>> GetAllLocations(int? page, int? perPage, int? organizationId = null, string sortOrder = null);
        Task UpdateLocation(int id, UpdateLocationRequest request);
    }
}
