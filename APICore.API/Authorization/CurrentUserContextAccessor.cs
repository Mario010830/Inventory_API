using APICore.API.Middleware;
using Microsoft.AspNetCore.Http;
using APICore.Common.DTO;
using APICore.Services;

namespace APICore.API.Authorization
{
    public class CurrentUserContextAccessor : ICurrentUserContextAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserContextAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public CurrentUserContext? GetCurrent()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context?.Items == null) return null;
            return context.Items.TryGetValue(CurrentUserContextMiddleware.CurrentUserContextKey, out var value) && value is CurrentUserContext ctx
                ? ctx
                : null;
        }
    }
}
