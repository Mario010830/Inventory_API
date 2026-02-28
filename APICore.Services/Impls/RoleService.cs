using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using APICore.Services.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<IRoleService> _localizer;

        public RoleService(IUnitOfWork uow, CoreDbContext context, IStringLocalizer<IRoleService> localizer)
        {
            _uow = uow;
            _context = context;
            _localizer = localizer;
        }

        public async Task<RoleResponse> CreateRole(CreateRoleRequest request)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var nameExists = await _uow.RoleRepository.FindBy(r => r.Name == request.Name && (r.OrganizationId == null || r.OrganizationId == orgId)).AnyAsync();
            if (nameExists)
            {
                throw new RoleNameInUseBadRequestException(_localizer);
            }

            var role = new Role
            {
                OrganizationId = orgId,
                Name = request.Name,
                Description = request.Description,
                IsSystem = false,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            await _uow.RoleRepository.AddAsync(role);
            await _uow.CommitAsync();

            await SetRolePermissionsAsync(role.Id, request.PermissionIds ?? new List<int>());
            await _uow.CommitAsync();
            return await GetRole(role.Id);
        }

        public async Task DeleteRole(int id)
        {
            var role = await _uow.RoleRepository.FirstOrDefaultAsync(r => r.Id == id);
            if (role == null)
                throw new RoleNotFoundException(_localizer);
            if (role.IsSystem)
                throw new RoleIsSystemCannotDeleteBadRequestException(_localizer);

            var usersWithRole = await _uow.UserRepository.FindBy(u => u.RoleId == id).AnyAsync();
            if (usersWithRole)
                throw new RoleInUseCannotDeleteBadRequestException(_localizer);

            var rolePerms = await _uow.RolePermissionRepository.FindBy(rp => rp.RoleId == id).ToListAsync();
            foreach (var rp in rolePerms)
                _uow.RolePermissionRepository.Delete(rp);
            _uow.RoleRepository.Delete(role);
            await _uow.CommitAsync();
        }

        public async Task<RoleResponse> GetRole(int id)
        {
            var role = await _uow.RoleRepository.FirstOrDefaultAsync(r => r.Id == id);
            if (role == null)
                throw new RoleNotFoundException(_localizer);

            var permissionIds = await _uow.RolePermissionRepository.FindBy(rp => rp.RoleId == id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            return ToRoleResponse(role, permissionIds);
        }

        public async Task<PaginatedList<RoleResponse>> GetAllRoles(int? page, int? perPage, string sortOrder = null)
        {
            var query = _uow.RoleRepository.GetAll();
            var pageIndex = page ?? 1;
            var perPageIndex = perPage ?? 10;
            var paged = await PaginatedList<Role>.CreateAsync(query, pageIndex, perPageIndex);

            var roleIds = paged.Select(r => r.Id).ToList();
            var allRolePerms = await _uow.RolePermissionRepository.FindBy(rp => roleIds.Contains(rp.RoleId))
                .ToListAsync();
            var permByRole = allRolePerms.GroupBy(rp => rp.RoleId).ToDictionary(g => g.Key, g => g.Select(rp => rp.PermissionId).ToList());

            var items = paged.Select(r => ToRoleResponse(r, permByRole.GetValueOrDefault(r.Id, new List<int>()))).ToList();
            return new PaginatedList<RoleResponse>(items, paged.TotalItems, pageIndex, perPageIndex);
        }

        public async Task UpdateRole(int id, UpdateRoleRequest request)
        {
            var role = await _uow.RoleRepository.FirstOrDefaultAsync(r => r.Id == id);
            if (role == null)
                throw new RoleNotFoundException(_localizer);

            if (request.Name != null)
            {
                var nameExists = await _uow.RoleRepository.FindBy(r => r.Name == request.Name && r.Id != id).AnyAsync();
                if (nameExists)
                    throw new RoleNameInUseBadRequestException(_localizer);
                role.Name = request.Name;
            }
            if (request.Description != null)
                role.Description = request.Description;

            role.ModifiedAt = DateTime.UtcNow;
            _uow.RoleRepository.Update(role);

            if (request.PermissionIds != null)
                await SetRolePermissionsAsync(id, request.PermissionIds);

            await _uow.CommitAsync();
        }

        public async Task<IEnumerable<PermissionResponse>> GetAllPermissions()
        {
            var list = await _uow.PermissionRepository.GetAll().OrderBy(p => p.Code).ToListAsync();
            return list.Select(p => new PermissionResponse { Id = p.Id, Code = p.Code, Name = p.Name, Description = p.Description });
        }

        private async Task SetRolePermissionsAsync(int roleId, List<int> permissionIds)
        {
            var existing = await _uow.RolePermissionRepository.FindBy(rp => rp.RoleId == roleId).ToListAsync();
            foreach (var rp in existing)
                _uow.RolePermissionRepository.Delete(rp);

            var validIds = await _uow.PermissionRepository.GetAll().Where(p => permissionIds.Contains(p.Id)).Select(p => p.Id).ToListAsync();
            foreach (var permId in validIds)
                await _uow.RolePermissionRepository.AddAsync(new RolePermission { RoleId = roleId, PermissionId = permId });
        }

        private static RoleResponse ToRoleResponse(Role role, List<int> permissionIds)
        {
            return new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsSystem = role.IsSystem,
                CreatedAt = role.CreatedAt,
                ModifiedAt = role.ModifiedAt,
                PermissionIds = permissionIds ?? new List<int>()
            };
        }
    }
}
