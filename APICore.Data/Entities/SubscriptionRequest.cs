using System;

namespace APICore.Data.Entities
{
    public class SubscriptionRequest : BaseEntity
    {
        public int SubscriptionId { get; set; }
        public virtual Subscription Subscription { get; set; }

        public string Type { get; set; }
        public string Status { get; set; }
        public string? Notes { get; set; }
        public string? PaymentReference { get; set; }
        public int? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
