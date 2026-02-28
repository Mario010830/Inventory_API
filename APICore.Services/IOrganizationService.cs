using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Services.Utils;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IOrganizationService
    {
        Task<OrganizationResponse> CreateOrganization(CreateOrganizationRequest request);
        Task DeleteOrganization(int id);
        Task<OrganizationResponse> GetOrganization(int id);
        Task<PaginatedList<OrganizationResponse>> GetAllOrganizations(int? page, int? perPage, string sortOrder = null);
        Task UpdateOrganization(int id, UpdateOrganizationRequest request);
    }
}
