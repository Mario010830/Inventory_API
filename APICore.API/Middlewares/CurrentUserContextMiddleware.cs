using APICore.Data;
using Microsoft.AspNetCore.Http;
using APICore.API.Utils;
using System.Threading.Tasks;
using APICore.Services;

namespace APICore.API.Middleware
{
    /// <summary>
    /// Carga el contexto del usuario (rol, ubicación, permisos), lo guarda en HttpContext.Items
    /// y configura el DbContext para multitenancy (filtros globales por LocationId).
    /// Debe ejecutarse después de UseAuthentication.
    /// </summary>
    public class CurrentUserContextMiddleware
    {
        public const string CurrentUserContextKey = "CurrentUserContext";
        private readonly RequestDelegate _next;

        public CurrentUserContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICurrentUserContextProvider provider)
        {
            var userId = context.User?.GetUserIdFromToken() ?? 0;

            if (userId > 0)
            {
                var userContext = await provider.GetAsync(userId);
                context.Items[CurrentUserContextKey] = userContext;

                var dbContext = context.RequestServices.GetService(typeof(CoreDbContext)) as CoreDbContext;
                if (dbContext != null)
                {
                    dbContext.IgnoreLocationFilter = userContext?.IsSuperAdmin == true;
                    dbContext.CurrentLocationId = userContext?.LocationId ?? -1;
                    dbContext.CurrentOrganizationId = userContext?.OrganizationId ?? -1;
                }
            }
            else
            {
                context.Items[CurrentUserContextKey] = null;
                var dbContext = context.RequestServices.GetService(typeof(CoreDbContext)) as CoreDbContext;
                if (dbContext != null)
                {
                    dbContext.IgnoreLocationFilter = false;
                    dbContext.CurrentLocationId = -1;
                    dbContext.CurrentOrganizationId = -1;
                }
            }

            await _next(context);
        }
    }
}
