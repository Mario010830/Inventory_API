using APICore.Common.DTO.Request;
using APICore.Services;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace APICore.API.Services
{
    public class LoyaltyThresholdPushNotifier : ILoyaltyThresholdNotifier
    {
        private readonly IPushNotificationService _pushNotificationService;
        private readonly ILogger<LoyaltyThresholdPushNotifier> _logger;

        public LoyaltyThresholdPushNotifier(
            IPushNotificationService pushNotificationService,
            ILogger<LoyaltyThresholdPushNotifier> logger)
        {
            _pushNotificationService = pushNotificationService;
            _logger = logger;
        }

        public async Task NotifyLoyaltyMilestoneAsync(int _organizationId, int locationId, int contactId, int lifetimeOrders, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _pushNotificationService.SendToLocationAsync(new PushSendRequest
                {
                    LocationId = locationId,
                    Title = "Cliente frecuente",
                    Body = $"Un cliente alcanzó {lifetimeOrders} compras confirmadas (contacto #{contactId}).",
                    Tag = "loyalty-milestone",
                });
                if (result.Failed > 0 && result.Sent == 0)
                    _logger.LogDebug("Loyalty push sin suscriptores o falló en ubicación {LocationId}.", locationId);
            }
            catch (System.Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar push de lealtad para contacto {ContactId}.", contactId);
            }
        }
    }
}
