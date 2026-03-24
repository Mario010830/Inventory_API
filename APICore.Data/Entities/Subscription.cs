using System;
using System.Collections.Generic;

namespace APICore.Data.Entities
{
    public class Subscription : BaseEntity
    {
        public int OrganizationId { get; set; }
        public virtual Organization Organization { get; set; }

        public int PlanId { get; set; }
        public virtual Plan Plan { get; set; }

        public string BillingCycle { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<SubscriptionRequest> Requests { get; set; } = new List<SubscriptionRequest>();
    }
}
