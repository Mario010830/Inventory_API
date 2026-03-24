using APICore.Common.Constants;
using APICore.Common.DTO;
using APICore.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class CurrentUserContextProvider : ICurrentUserContextProvider
    {
        private readonly CoreDbContext _context;

        public CurrentUserContextProvider(CoreDbContext context)
        {
            _context = context;
        }

        public async Task<CurrentUserContext?> GetAsync(int userId)
        {
            if (userId <= 0) return null;

            var user = await _context.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(u => u.Role)
                    .ThenInclude(r => r!.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .Include(u => u.Location)
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            var permissionCodes = user.Role?.RolePermissions?
                .Select(rp => rp.Permission?.Code)
                .Where(c => c != null)
                .Cast<string>()
                .ToList() ?? new System.Collections.Generic.List<string>();

            var isSuperAdmin = string.Equals(user.Role?.Name, RoleNames.SuperAdmin, System.StringComparison.OrdinalIgnoreCase);
            var isOrgAdmin = string.Equals(user.Role?.Name, RoleNames.Admin, System.StringComparison.OrdinalIgnoreCase);
            var isAdmin = user.Role?.IsSystem == true || isSuperAdmin || isOrgAdmin;

            int? organizationId = user.OrganizationId ?? user.Location?.OrganizationId;

            // SuperAdmin del seed suele tener OrganizationId null; los servicios (categorías, productos) exigen org > 0.
            if (organizationId == null && isSuperAdmin)
            {
                var defaultOrg = await _context.Organizations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Code == "DEFAULT");
                organizationId = defaultOrg?.Id;
            }

            return new CurrentUserContext
            {
                UserId = user.Id,
                LocationId = user.LocationId,
                OrganizationId = organizationId,
                RoleId = user.RoleId,
                IsSuperAdmin = isSuperAdmin,
                IsAdmin = isAdmin,
                PermissionCodes = permissionCodes
            };
        }
    }
}
