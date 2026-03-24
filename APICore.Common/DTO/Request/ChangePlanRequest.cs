using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class ChangePlanRequest
    {
        [Required]
        public int PlanId { get; set; }

        [Required]
        public string BillingCycle { get; set; }
        public string? Notes { get; set; }
        public string? PaymentReference { get; set; }
    }
}
