using APICore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace APICore.API.Authorization
{
    /// <summary>
    /// Solo usuarios con rol SuperAdmin (sin alcance de organización global en el modelo de seguridad).
    /// </summary>
    public class RequireSuperAdminAttribute : TypeFilterAttribute
    {
        public RequireSuperAdminAttribute() : base(typeof(RequireSuperAdminFilter))
        {
        }
    }

    public class RequireSuperAdminFilter : IAsyncAuthorizationFilter
    {
        private readonly ICurrentUserContextAccessor _currentUserContextAccessor;

        public RequireSuperAdminFilter(ICurrentUserContextAccessor currentUserContextAccessor)
        {
            _currentUserContextAccessor = currentUserContextAccessor;
        }

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userContext = _currentUserContextAccessor.GetCurrent();
            if (userContext?.IsSuperAdmin != true)
                context.Result = new ForbidResult();
            return Task.CompletedTask;
        }
    }
}
