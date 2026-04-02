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

        /// <summary>Listado paginado de todas las organizaciones (uso panel superadmin).</summary>
        Task<PaginatedList<OrganizationResponse>> GetAllOrganizationsForSuperAdmin(int? page, int? perPage, string sortOrder = null);

        /// <summary>Establece verificación de la organización y replica el valor en todas sus localizaciones.</summary>
        Task SetOrganizationVerification(int organizationId, bool isVerified);
    }
}
