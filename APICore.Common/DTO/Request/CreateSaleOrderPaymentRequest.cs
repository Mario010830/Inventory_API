using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class CreateSaleOrderPaymentRequest
    {
        [Required]
        public int PaymentMethodId { get; set; }

        /// <summary>Aporte en CUP (moneda base) a la venta.</summary>
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [MaxLength(120)]
        public string? Reference { get; set; }

        /// <summary>Moneda de cobro; null = solo CUP sin desglose extranjero.</summary>
        public int? CurrencyId { get; set; }

        /// <summary>Importe neto en <see cref="CurrencyId"/> (tras vuelto).</summary>
        public decimal? AmountForeign { get; set; }

        /// <summary>Tasa CUP por 1 unidad de moneda; debe coincidir con la tasa vigente en servidor (tolerancia).</summary>
        public decimal? ExchangeRateSnapshot { get; set; }

        /// <summary>Billetes/monedas entregados por el cliente (efectivo).</summary>
        public List<SaleOrderPaymentDenominationLineRequest>? TenderDenominations { get; set; }

        /// <summary>Billetes/monedas devueltos como vuelto.</summary>
        public List<SaleOrderPaymentDenominationLineRequest>? ChangeDenominations { get; set; }
    }
}
