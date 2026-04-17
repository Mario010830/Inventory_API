using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class SaleOrderPayment : BaseEntity
    {
        [Required]
        public int SaleOrderId { get; set; }

        [Required]
        public int PaymentMethodId { get; set; }

        public decimal Amount { get; set; }

        public SaleOrder? SaleOrder { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
    }
}
