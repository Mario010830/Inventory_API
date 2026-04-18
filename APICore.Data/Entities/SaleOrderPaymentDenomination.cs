using APICore.Data.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class SaleOrderPaymentDenomination : BaseEntity
    {
        [Required]
        public int SaleOrderPaymentId { get; set; }

        public SaleOrderPayment SaleOrderPayment { get; set; } = null!;

        public SaleOrderPaymentDenominationKind Kind { get; set; }

        /// <summary>Valor facial entregado o devuelto.</summary>
        public decimal Value { get; set; }

        public int Quantity { get; set; }
    }
}
