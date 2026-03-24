using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data.Entities;
using APICore.Services.Utils;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface ISubscriptionService
    {
        Task<Subscription> CreateFreeSubscriptionAsync(int organizationId);
        Task<SubscriptionRequest> CreatePaidSubscriptionRequestAsync(int organizationId, int planId, string billingCycle);
        Task<SubscriptionRequest> ApproveRequestAsync(int requestId, ApproveSubscriptionRequestDto dto, int reviewerUserId);
        Task<SubscriptionRequest> RejectRequestAsync(int requestId, RejectSubscriptionRequestDto dto, int reviewerUserId);
        Task<Subscription> RenewSubscriptionAsync(int subscriptionId, RenewSubscriptionRequest dto, int reviewerUserId);
        Task<Subscription> ChangePlanAsync(int subscriptionId, ChangePlanRequest dto, int reviewerUserId);
        Task CheckAndExpireSubscriptionsAsync();
        Task<SubscriptionResponse> GetMySubscriptionAsync(int organizationId);
        Task<PaginatedList<SubscriptionRequest>> GetSubscriptionRequestsAsync(int? page, int? perPage, string statusFilter);
        Task<SubscriptionRequest> GetSubscriptionRequestByIdAsync(int id);
        Task<PaginatedList<Subscription>> GetAllSubscriptionsAsync(int? page, int? perPage, string statusFilter, int? planId);
        Task<Subscription> GetSubscriptionByIdAsync(int id);
    }
}
