using System;

namespace APICore.Data.Entities
{
    public class CustomerLoyaltyAccount : BaseEntity
    {
        public int OrganizationId { get; set; }
        public int ContactId { get; set; }
        public int PointsBalance { get; set; }
        public int LifetimeOrders { get; set; }
        public DateTime? LastPurchaseAt { get; set; }

        public virtual Organization Organization { get; set; } = null!;
        public virtual Contact Contact { get; set; } = null!;
    }
}
