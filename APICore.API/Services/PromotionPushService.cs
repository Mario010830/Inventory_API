using APICore.Data;
using APICore.Common.DTO.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.API.Services
{
    public class PromotionPushService : IPromotionPushService
    {
        private readonly CoreDbContext _context;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PromotionPushService> _logger;

        public PromotionPushService(
            CoreDbContext context,
            IPushNotificationService pushNotificationService,
            IConfiguration configuration,
            ILogger<PromotionPushService> logger)
        {
            _context = context;
            _pushNotificationService = pushNotificationService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// If <paramref name="pathOrUrl"/> is already absolute, returns it; otherwise joins with <paramref name="baseUrl"/> (PWA/API public base).
        /// </summary>
        private static string? ToAbsoluteUrl(string? baseUrl, string? pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
                return pathOrUrl;
            if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out _))
                return pathOrUrl;
            var b = (baseUrl ?? "").TrimEnd('/');
            if (string.IsNullOrEmpty(b))
                return pathOrUrl;
            return $"{b}/{pathOrUrl.TrimStart('/')}";
        }

        /// <summary>
        /// Resolves the product image to an HTTPS URL for Web Push (e.g. Vercel proxy). Uses
        /// <c>PushNotifications:PushImageProxyUrlTemplate</c> with <c>{url}</c> (full source URL, encoded) or <c>{path}</c> (PathAndQuery only, encoded).
        /// If the template is empty, returns the absolute URL from LocalStorage (legacy HTTP API URL).
        /// </summary>
        private string? BuildPushProductImageUrl(string? apiBase, string? imagenUrl)
        {
            var sourceAbsolute = ToAbsoluteUrl(apiBase, imagenUrl);
            if (string.IsNullOrWhiteSpace(sourceAbsolute))
                return null;

            var template = _configuration["PushNotifications:PushImageProxyUrlTemplate"]?.Trim();
            if (string.IsNullOrEmpty(template))
                return sourceAbsolute;

            const string urlToken = "{url}";
            const string pathToken = "{path}";

            if (template.Contains(pathToken, StringComparison.Ordinal))
            {
                if (!Uri.TryCreate(sourceAbsolute, UriKind.Absolute, out var srcUri))
                    return sourceAbsolute;
                var pathAndQuery = string.IsNullOrEmpty(srcUri.PathAndQuery) ? "/" : srcUri.PathAndQuery;
                return template.Replace(pathToken, Uri.EscapeDataString(pathAndQuery), StringComparison.Ordinal);
            }

            if (template.Contains(urlToken, StringComparison.Ordinal))
                return template.Replace(urlToken, Uri.EscapeDataString(sourceAbsolute), StringComparison.Ordinal);

            _logger.LogWarning(
                "PushImageProxyUrlTemplate is set but has no {{url}} or {{path}} placeholder; using raw image URL.");
            return sourceAbsolute;
        }

        public async Task<PromotionPushDispatchResult> NotifyPromotionActivatedAsync(int promotionId)
        {
            var dispatch = new PromotionPushDispatchResult
            {
                PushAttempted = false,
                PromotionId = promotionId
            };

            var promotion = await _context.Promotions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == promotionId);
            if (promotion == null || !promotion.IsActive)
                return dispatch;

            dispatch.OrganizationId = promotion.OrganizationId;

            var product = await _context.Products
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == promotion.ProductId && p.OrganizationId == promotion.OrganizationId);
            if (product == null)
                return dispatch;

            var locationIds = await _context.Locations
                .IgnoreQueryFilters()
                .Where(l => l.OrganizationId == promotion.OrganizationId)
                .Select(l => l.Id)
                .ToListAsync();

            dispatch.ResolvedLocationsCount = locationIds.Count;
            if (locationIds.Count == 0)
                return dispatch;

            dispatch.PushAttempted = true;

            var storeBase = _configuration["PushNotifications:PublicStoreBaseUrl"];
            var apiBase = _configuration["LocalStorage:BaseUrl"];

            var promoLabel = promotion.Type.ToString() == "percentage"
                ? $"{promotion.Value:0.##}% OFF"
                : $"Oferta {promotion.Value:0.##}";

            foreach (var locationId in locationIds)
            {
                try
                {
                    var relativeStorePath = $"/store?locationId={locationId}&productId={product.Id}";
                    var pushImage = BuildPushProductImageUrl(apiBase, product.ImagenUrl);
                    var payload = new PushSendRequest
                    {
                        LocationId = locationId,
                        Title = "Nueva promocion disponible",
                        Body = $"{product.Name}: {promoLabel}",
                        Url = ToAbsoluteUrl(storeBase, relativeStorePath) ?? relativeStorePath,
                        Tag = $"promo-location-{locationId}",
                        Image = pushImage,
                        ImageUrl = pushImage,
                        Icon = ToAbsoluteUrl(storeBase, "/images/icon-192x192.png"),
                        Badge = ToAbsoluteUrl(storeBase, "/images/icon-72x72.png")
                    };

                    var response = await _pushNotificationService.SendToLocationAsync(payload);
                    dispatch.Sent += response.Sent;
                    dispatch.Failed += response.Failed;
                    dispatch.Deactivated += response.Deactivated;
                    dispatch.Locations.Add(new PromotionPushLocationResult
                    {
                        LocationId = locationId,
                        TotalSubscriptions = response.TotalSubscriptions,
                        Sent = response.Sent,
                        Failed = response.Failed,
                        Deactivated = response.Deactivated,
                        Error = response.Error
                    });

                    if (response.Failed > 0)
                    {
                        _logger.LogWarning(
                            "Push send partial failure for promotion {PromotionId}, location {LocationId}. Sent {Sent}, Failed {Failed}",
                            promotionId, locationId, response.Sent, response.Failed);
                    }
                }
                catch (Exception ex)
                {
                    dispatch.Failed++;
                    dispatch.Locations.Add(new PromotionPushLocationResult
                    {
                        LocationId = locationId,
                        TotalSubscriptions = 0,
                        Sent = 0,
                        Failed = 1,
                        Deactivated = 0,
                        Error = ex.Message
                    });
                    _logger.LogWarning(ex,
                        "Push send exception for promotion {PromotionId}, location {LocationId}",
                        promotionId, locationId);
                }
            }

            return dispatch;
        }
    }
}
