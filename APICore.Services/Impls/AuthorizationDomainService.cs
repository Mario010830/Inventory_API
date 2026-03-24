using APICore.Common.Constants;
using APICore.Common.DTO;
using System.Linq;

namespace APICore.Services.Impls
{

    public class AuthorizationDomainService : IAuthorizationDomainService
    {
        public bool HasPermission(CurrentUserContext? userContext, string permissionCode)
        {
            if (userContext == null) return false;
            if (string.IsNullOrEmpty(permissionCode)) return true;

            if (userContext.IsAdmin) return true;
            if (ContainsPermission(userContext, PermissionCodes.Admin)) return true;
            return ContainsPermission(userContext, permissionCode);
        }

        public bool HasAnyPermission(CurrentUserContext? userContext, string[] permissionCodes)
        {
            if (userContext == null || permissionCodes == null || permissionCodes.Length == 0) return false;
            if (userContext.IsAdmin || ContainsPermission(userContext, PermissionCodes.Admin)) return true;
            return permissionCodes.Any(code => !string.IsNullOrEmpty(code) && ContainsPermission(userContext, code));
        }

        private static bool ContainsPermission(CurrentUserContext userContext, string permissionCode)
        {
            return userContext.PermissionCodes != null && userContext.PermissionCodes.Contains(permissionCode);
        }
    }
}
