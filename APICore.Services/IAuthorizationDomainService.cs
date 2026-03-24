using APICore.Common.DTO;

namespace APICore.Services
{
    public interface IAuthorizationDomainService
    {
        bool HasPermission(CurrentUserContext? userContext, string permissionCode);

        /// <summary>True si el usuario tiene al menos uno de los permisos (para listas/desplegables que necesitan otros módulos).</summary>
        bool HasAnyPermission(CurrentUserContext? userContext, string[] permissionCodes);
    }
}
