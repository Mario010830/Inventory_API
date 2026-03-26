using System.Threading.Tasks;

namespace APICore.API.Services
{
    public interface IPromotionPushService
    {
        Task NotifyPromotionActivatedAsync(int promotionId);
    }
}
