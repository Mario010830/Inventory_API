using System.Threading.Tasks;

namespace APICore.API.Services
{
    public class PromotionPushLocationResult
    {
        public int LocationId { get; set; }
        public int TotalSubscriptions { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public int Deactivated { get; set; }
        public string? Error { get; set; }
    }

    public class PromotionPushDispatchResult
    {
        public bool PushAttempted { get; set; }
        public int PromotionId { get; set; }
        public int OrganizationId { get; set; }
        public int ResolvedLocationsCount { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public int Deactivated { get; set; }
        public System.Collections.Generic.List<PromotionPushLocationResult> Locations { get; set; } = new();
    }

    public interface IPromotionPushService
    {
        Task<PromotionPushDispatchResult> NotifyPromotionActivatedAsync(int promotionId);
    }
}
