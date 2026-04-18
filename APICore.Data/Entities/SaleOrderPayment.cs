using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class SaleOrderPayment : BaseEntity
    {
        [Required]
        public int SaleOrderId { get; set; }

        [Required]
        public int PaymentMethodId { get; set; }

        /// <summary>Aporte en CUP (moneda base) a la venta.</summary>
        public decimal Amount { get; set; }

        /// <summary>Referencia por línea de pago (p. ej. últimos 4 de tarjeta, terminal, cuenta enmascarada, folio SPEI)
        /// para cuadre y desglose; no usar para almacenar PAN completo.</summary>
        [MaxLength(120)]
        public string? Reference { get; set; }

        /// <summary>Moneda de cobro cuando aplica multimoneda; null = solo CUP sin desglose extranjero.</summary>
        public int? CurrencyId { get; set; }

        /// <summary>Importe neto cobrado en <see cref="CurrencyId"/> (tras vuelto).</summary>
        public decimal? AmountForeign { get; set; }

        /// <summary>Tasa CUP por 1 unidad de moneda extranjera al momento del registro.</summary>
        public decimal? ExchangeRateSnapshot { get; set; }

        public SaleOrder? SaleOrder { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public Currency? Currency { get; set; }

        public ICollection<SaleOrderPaymentDenomination> Denominations { get; set; } = new List<SaleOrderPaymentDenomination>();
    }
}
