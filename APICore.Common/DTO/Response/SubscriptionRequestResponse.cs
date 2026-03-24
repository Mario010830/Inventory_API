using System;

namespace APICore.Common.DTO.Response
{
    public class SubscriptionRequestResponse
    {
        public int Id { get; set; }
        public int SubscriptionId { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string? Notes { get; set; }
        public string? PaymentReference { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public SubscriptionResponse Subscription { get; set; }
        public OrganizationResponse? Organization { get; set; }
    }
}
