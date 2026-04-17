using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class CreateSaleOrderPaymentRequest
    {
        [Required]
        public int PaymentMethodId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [MaxLength(120)]
        public string? Reference { get; set; }
    }
}
