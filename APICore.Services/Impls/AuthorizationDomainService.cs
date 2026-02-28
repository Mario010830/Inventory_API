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

        private static bool ContainsPermission(CurrentUserContext userContext, string permissionCode)
        {
            return userContext.PermissionCodes != null && userContext.PermissionCodes.Contains(permissionCode);
        }
    }
}
