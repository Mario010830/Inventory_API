using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class RenewSubscriptionRequest
    {
        [Required]
        public string BillingCycle { get; set; }
        public string? PaymentReference { get; set; }
        public string? Notes { get; set; }
    }
}
