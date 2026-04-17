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

        /// <summary>
        /// Referencia por línea de pago (p. ej. últimos 4 de tarjeta, terminal, cuenta enmascarada, folio SPEI)
        /// para cuadre y desglose; no usar para almacenar PAN completo.
        /// </summary>
        [MaxLength(120)]
        public string? Reference { get; set; }

        public SaleOrder? SaleOrder { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
    }
}
