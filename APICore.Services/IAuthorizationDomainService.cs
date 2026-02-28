using APICore.Common.DTO;

namespace APICore.Services
{

    public interface IAuthorizationDomainService
    {

        bool HasPermission(CurrentUserContext? userContext, string permissionCode);
    }
}
