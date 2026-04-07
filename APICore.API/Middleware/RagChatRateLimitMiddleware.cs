using System;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace APICore.API.Middleware
{
    /// <summary>Límite fijo 20 solicitudes por minuto por IP solo para POST /api/chat/ask.</summary>
    public sealed class RagChatRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly PartitionedRateLimiter<HttpContext> _limiter;

        public RagChatRateLimitMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _limiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            {
                var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    ip,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 20,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    });
            });
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (HttpMethods.IsPost(context.Request.Method) &&
                context.Request.Path.StartsWithSegments("/api/chat/ask"))
            {
                using var lease = await _limiter.AcquireAsync(context, permitCount: 1, context.RequestAborted).ConfigureAwait(false);
                if (!lease.IsAcquired)
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    return;
                }
            }

            await _next(context).ConfigureAwait(false);
        }
    }
}
