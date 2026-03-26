using APICore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace APICore.API.Services
{
    public class PromotionPushService : IPromotionPushService
    {
        private readonly CoreDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PromotionPushService> _logger;

        public PromotionPushService(
            CoreDbContext context,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<PromotionPushService> logger)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task NotifyPromotionActivatedAsync(int promotionId)
        {
            var sendEndpoint = _configuration["PushNotifications:SendEndpoint"];
            if (string.IsNullOrWhiteSpace(sendEndpoint))
                return;

            var promotion = await _context.Promotions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == promotionId);
            if (promotion == null || !promotion.IsActive)
                return;

            var product = await _context.Products
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == promotion.ProductId && p.OrganizationId == promotion.OrganizationId);
            if (product == null)
                return;

            var locationIds = await _context.Locations
                .IgnoreQueryFilters()
                .Where(l => l.OrganizationId == promotion.OrganizationId)
                .Select(l => l.Id)
                .ToListAsync();

            if (locationIds.Count == 0)
                return;

            var promoLabel = promotion.Type.ToString() == "percentage"
                ? $"{promotion.Value:0.##}% OFF"
                : $"Oferta {promotion.Value:0.##}";

            foreach (var locationId in locationIds)
            {
                try
                {
                    var payload = new
                    {
                        locationId,
                        title = "Nueva promocion disponible",
                        body = $"{product.Name}: {promoLabel}",
                        url = $"/store?locationId={locationId}&productId={product.Id}"
                    };

                    var response = await _httpClient.PostAsJsonAsync(sendEndpoint, payload);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning(
                            "Push send failed for promotion {PromotionId}, location {LocationId}, status {StatusCode}",
                            promotionId, locationId, (int)response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Push send exception for promotion {PromotionId}, location {LocationId}",
                        promotionId, locationId);
                }
            }
        }
    }
}
