using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data;
using APICore.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using WebPush;

namespace APICore.API.Services
{
    public class PushNotificationService : IPushNotificationService
    {
        private readonly CoreDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PushNotificationService> _logger;

        public PushNotificationService(
            CoreDbContext context,
            IConfiguration configuration,
            ILogger<PushNotificationService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task UpsertSubscriptionAsync(PushSubscribeRequest request)
        {
            var location = await _context.Locations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.Id == request.LocationId);

            if (location == null)
                throw new InvalidOperationException("LocationId invalido para suscripcion push.");

            var existing = await _context.WebPushSubscriptions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Endpoint == request.Endpoint);

            if (existing == null)
            {
                existing = new WebPushSubscription
                {
                    Endpoint = request.Endpoint,
                    P256DH = request.Keys.P256dh,
                    Auth = request.Keys.Auth,
                    ExpirationTime = request.ExpirationTime,
                    LocationId = request.LocationId,
                    OrganizationId = location.OrganizationId,
                    IsActive = true
                };
                await _context.WebPushSubscriptions.AddAsync(existing);
            }
            else
            {
                existing.P256DH = request.Keys.P256dh;
                existing.Auth = request.Keys.Auth;
                existing.ExpirationTime = request.ExpirationTime;
                existing.LocationId = request.LocationId;
                existing.OrganizationId = location.OrganizationId;
                existing.IsActive = true;
                _context.WebPushSubscriptions.Update(existing);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<PushSendResultResponse> SendToLocationAsync(PushSendRequest request)
        {
            var subscriptions = await _context.WebPushSubscriptions
                .IgnoreQueryFilters()
                .Where(s => s.IsActive && s.LocationId == request.LocationId)
                .ToListAsync();

            var result = new PushSendResultResponse
            {
                LocationId = request.LocationId,
                TotalSubscriptions = subscriptions.Count,
                Sent = 0,
                Failed = 0,
                Deactivated = 0,
                Error = null
            };

            if (subscriptions.Count == 0)
                return result;

            var subject = _configuration["PushNotifications:VapidSubject"];
            var publicKey = _configuration["PushNotifications:VapidPublicKey"];
            var privateKey = _configuration["PushNotifications:VapidPrivateKey"];
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
            {
                _logger.LogWarning("Push VAPID keys are not configured.");
                result.Failed = result.TotalSubscriptions;
                result.Error = "VAPID keys are not configured.";
                return result;
            }

            var webPushClient = new WebPushClient();
            var vapid = new VapidDetails(subject, publicKey, privateKey);
            var payload = JsonSerializer.Serialize(new
            {
                title = request.Title,
                body = request.Body,
                url = request.Url,
                locationId = request.LocationId,
                image = !string.IsNullOrWhiteSpace(request.Image) ? request.Image : request.ImageUrl,
                imageUrl = !string.IsNullOrWhiteSpace(request.ImageUrl) ? request.ImageUrl : request.Image,
                tag = request.Tag,
                icon = request.Icon,
                badge = request.Badge
            });

            _logger.LogInformation(
                "Push payload prepared for location {LocationId}: {Payload}",
                request.LocationId,
                payload);

            foreach (var sub in subscriptions)
            {
                try
                {
                    var pushSub = new PushSubscription(sub.Endpoint, sub.P256DH, sub.Auth);
                    await webPushClient.SendNotificationAsync(pushSub, payload, vapid);
                    result.Sent++;
                }
                catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.NotFound)
                {
                    sub.IsActive = false;
                    _context.WebPushSubscriptions.Update(sub);
                    result.Failed++;
                    result.Deactivated++;
                    result.Error ??= $"WebPush {(int)ex.StatusCode} {ex.StatusCode}";
                }
                catch (WebPushException ex)
                {
                    _logger.LogWarning(ex,
                        "WebPush failed for location {LocationId} with status {StatusCode}",
                        request.LocationId, (int)ex.StatusCode);
                    result.Failed++;
                    result.Error ??= $"WebPush {(int)ex.StatusCode} {ex.StatusCode}";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed sending push notification to location {LocationId}", request.LocationId);
                    result.Failed++;
                    result.Error ??= ex.Message;
                }
            }

            if (result.Deactivated > 0)
                await _context.SaveChangesAsync();

            return result;
        }
    }
}
