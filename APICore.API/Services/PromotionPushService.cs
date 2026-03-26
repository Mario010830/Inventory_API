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
        /// Joins root-relative paths (<c>/store?...</c>, <c>/images/...</c>) with <paramref name="baseUrl"/>.
        /// Leaves <c>http(s)://...</c> unchanged. Root-relative must not use <see cref="UriKind.Absolute"/> alone — on Linux, <c>/store</c> parses as a file URI and would skip the base.
        /// </summary>
        private static string? ToAbsoluteUrl(string? baseUrl, string? pathOrUrl)
        {
            if (string.IsNullOrWhiteSpace(pathOrUrl))
                return pathOrUrl;

            var s = pathOrUrl.Trim();
            // Root-relative: always prefix with public base (notifications / PWA).
            if (s.StartsWith('/') && !s.StartsWith("//", StringComparison.Ordinal))
            {
                var b = (baseUrl ?? "").TrimEnd('/');
                return string.IsNullOrEmpty(b) ? pathOrUrl : $"{b}{s}";
            }

            if (Uri.TryCreate(s, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                return s;

            var b2 = (baseUrl ?? "").TrimEnd('/');
            if (string.IsNullOrEmpty(b2))
                return pathOrUrl;
            return $"{b2}/{s.TrimStart('/')}";
        }

        /// <summary>
        /// Product or location image via Next: <c>{PublicStoreBaseUrl}/api/image?path={encodeURIComponent(/uploads/...)}</c>.
        /// </summary>
        private string? BuildFrontendProxiedImageUrl(string? frontendBase, string? apiBase, string? imagenUrl)
        {
            var sourceAbsolute = ToAbsoluteUrl(apiBase, imagenUrl);
            if (string.IsNullOrWhiteSpace(sourceAbsolute))
                return null;

            if (string.IsNullOrEmpty(frontendBase))
            {
                _logger.LogWarning("Push image: PublicStoreBaseUrl is missing; using backend image URL.");
                return sourceAbsolute;
            }

            const string uploadsMarker = "/uploads/";
            var idx = sourceAbsolute.IndexOf(uploadsMarker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                _logger.LogWarning(
                    "Push image: backend URL has no {Marker} segment, using raw URL: {Url}",
                    uploadsMarker,
                    sourceAbsolute);
                return sourceAbsolute;
            }

            var path = sourceAbsolute.Substring(idx);
            var encoded = Uri.EscapeDataString(path);
            return $"{frontendBase}/api/image?path={encoded}";
        }

        /// <summary>
        /// Frontend base URL (Vercel / Next), e.g. <c>PushNotifications:PublicStoreBaseUrl</c>.
        /// </summary>
        private string? ResolvePublicStoreBaseUrl()
        {
            var configured = _configuration["PushNotifications:PublicStoreBaseUrl"]?.Trim();
            return string.IsNullOrEmpty(configured) ? null : configured.TrimEnd('/');
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

            var locations = await _context.Locations
                .IgnoreQueryFilters()
                .Where(l => l.OrganizationId == promotion.OrganizationId)
                .ToListAsync();

            dispatch.ResolvedLocationsCount = locations.Count;
            if (locations.Count == 0)
                return dispatch;

            dispatch.PushAttempted = true;

            var storeBase = ResolvePublicStoreBaseUrl();
            var apiBase = _configuration["LocalStorage:BaseUrl"];

            var promoLabel = promotion.Type.ToString() == "percentage"
                ? $"{promotion.Value:0.##}% OFF"
                : $"Oferta {promotion.Value:0.##}";

            foreach (var location in locations)
            {
                var locationId = location.Id;
                try
                {
                    var storeDisplayName = !string.IsNullOrWhiteSpace(location.Name)
                        ? location.Name.Trim()
                        : (!string.IsNullOrWhiteSpace(location.Code) ? location.Code.Trim() : "Tienda");

                    var relativeStorePath = $"/store?locationId={locationId}&productId={product.Id}";
                    var pushImage = BuildFrontendProxiedImageUrl(storeBase, apiBase, product.ImagenUrl);
                    var storeIcon = BuildFrontendProxiedImageUrl(storeBase, apiBase, location.PhotoUrl);
                    var defaultIcon = ToAbsoluteUrl(storeBase, "/images/icon-192x192.png");
                    var defaultBadge = ToAbsoluteUrl(storeBase, "/images/icon-72x72.png");

                    var payload = new PushSendRequest
                    {
                        LocationId = locationId,
                        StoreName = storeDisplayName,
                        Title = $"Nueva promocion — {storeDisplayName}",
                        Body = $"{product.Name}: {promoLabel}",
                        Url = ToAbsoluteUrl(storeBase, relativeStorePath) ?? relativeStorePath,
                        Tag = $"promo-location-{locationId}",
                        Image = pushImage,
                        ImageUrl = pushImage,
                        Icon = !string.IsNullOrWhiteSpace(storeIcon) ? storeIcon : defaultIcon,
                        Badge = !string.IsNullOrWhiteSpace(storeIcon) ? storeIcon : defaultBadge
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
