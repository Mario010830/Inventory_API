using APICore.Data;
using APICore.Common.DTO.Request;
using Microsoft.EntityFrameworkCore;
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
        private readonly ILogger<PromotionPushService> _logger;

        public PromotionPushService(
            CoreDbContext context,
            IPushNotificationService pushNotificationService,
            ILogger<PromotionPushService> logger)
        {
            _context = context;
            _pushNotificationService = pushNotificationService;
            _logger = logger;
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

            var promoLabel = promotion.Type.ToString() == "percentage"
                ? $"{promotion.Value:0.##}% OFF"
                : $"Oferta {promotion.Value:0.##}";

            foreach (var locationId in locationIds)
            {
                try
                {
                    var payload = new PushSendRequest
                    {
                        LocationId = locationId,
                        Title = "Nueva promocion disponible",
                        Body = $"{product.Name}: {promoLabel}",
                        Url = $"/store?locationId={locationId}&productId={product.Id}"
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
                        Deactivated = response.Deactivated
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
