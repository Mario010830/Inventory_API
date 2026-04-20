using System;

namespace APICore.Common.DTO.Response
{
    public class CustomerLoyaltyAccountResponse
    {
        public int ContactId { get; set; }
        public int PointsBalance { get; set; }
        public int LifetimeOrders { get; set; }
        public DateTime? LastPurchaseAt { get; set; }
        public int NotifyEveryNOrders { get; set; }
        public int? OrdersUntilNextMilestone { get; set; }
    }
}
