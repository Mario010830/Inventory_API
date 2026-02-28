using APICore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace APICore.API.Authorization
{

    public class RequirePermissionAttribute : TypeFilterAttribute
    {
        public RequirePermissionAttribute(string permissionCode) : base(typeof(RequirePermissionFilter))
        {
            Arguments = new object[] { permissionCode };
        }
    }

    public class RequirePermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly string _permissionCode;
        private readonly ICurrentUserContextAccessor _currentUserContextAccessor;
        private readonly IAuthorizationDomainService _authorizationDomainService;

        public RequirePermissionFilter(
            string permissionCode,
            ICurrentUserContextAccessor currentUserContextAccessor,
            IAuthorizationDomainService authorizationDomainService)
        {
            _permissionCode = permissionCode;
            _currentUserContextAccessor = currentUserContextAccessor;
            _authorizationDomainService = authorizationDomainService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            await Task.CompletedTask;

            var userContext = _currentUserContextAccessor.GetCurrent();
            var allowed = _authorizationDomainService.HasPermission(userContext, _permissionCode);

            if (!allowed)
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
