using APICore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading.Tasks;

namespace APICore.API.Authorization
{
    /// <summary>
    /// Exige al menos uno de los permisos indicados (útil para listas/desplegables: ej. ver categorías si puede crear productos).
    /// Un solo permiso = comportamiento habitual; varios = acceso si tiene cualquiera.
    /// </summary>
    public class RequirePermissionAttribute : TypeFilterAttribute
    {
        public RequirePermissionAttribute(params string[] permissionCodes) : base(typeof(RequirePermissionFilter))
        {
            if (permissionCodes == null || permissionCodes.Length == 0)
                throw new ArgumentException("Al menos un permiso es requerido.", nameof(permissionCodes));
            Arguments = new object[] { permissionCodes };
        }
    }

    public class RequirePermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly string[] _permissionCodes;
        private readonly ICurrentUserContextAccessor _currentUserContextAccessor;
        private readonly IAuthorizationDomainService _authorizationDomainService;

        public RequirePermissionFilter(
            string[] permissionCodes,
            ICurrentUserContextAccessor currentUserContextAccessor,
            IAuthorizationDomainService authorizationDomainService)
        {
            _permissionCodes = permissionCodes ?? throw new ArgumentNullException(nameof(permissionCodes));
            _currentUserContextAccessor = currentUserContextAccessor;
            _authorizationDomainService = authorizationDomainService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            await Task.CompletedTask;

            var userContext = _currentUserContextAccessor.GetCurrent();
            var allowed = _permissionCodes.Length == 1
                ? _authorizationDomainService.HasPermission(userContext, _permissionCodes[0])
                : _authorizationDomainService.HasAnyPermission(userContext, _permissionCodes);

            if (!allowed)
                context.Result = new ForbidResult();
        }
    }
}
