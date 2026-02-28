using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data.Entities;
using APICore.Services.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IRoleService
    {
        Task<RoleResponse> CreateRole(CreateRoleRequest request);
        Task DeleteRole(int id);
        Task<RoleResponse> GetRole(int id);
        Task<PaginatedList<RoleResponse>> GetAllRoles(int? page, int? perPage, string sortOrder = null);
        Task UpdateRole(int id, UpdateRoleRequest request);
        Task<IEnumerable<PermissionResponse>> GetAllPermissions();
    }
}
