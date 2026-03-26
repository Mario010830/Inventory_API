using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using System.Threading.Tasks;

namespace APICore.API.Services
{
    public interface IPushNotificationService
    {
        Task UpsertSubscriptionAsync(PushSubscribeRequest request);
        Task<PushSendResultResponse> SendToLocationAsync(PushSendRequest request);
    }
}
