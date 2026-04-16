using APICore.Common.Enums;
using System;
using System.Collections.Generic;

namespace APICore.Data.Entities
{
    /// <summary>
    /// Préstamo o inversión recuperable: capital prestado y pagos registrados por el cliente.
    /// </summary>
    public class Loan : BaseEntity
    {
        public int OrganizationId { get; set; }
        public Organization Organization { get; set; } = null!;

        /// <summary>Persona o entidad que debe.</summary>
        public string DebtorName { get; set; } = null!;

        /// <summary>Capital prestado (principal).</summary>
        /// <remarks>El importe está expresado en la moneda <see cref="PrincipalCurrency"/> cuando existe; si no, el cliente puede usar su moneda de visualización por defecto.</remarks>
        public decimal PrincipalAmount { get; set; }

        /// <summary>Moneda del capital prestado (opcional; préstamos antiguos pueden tener null).</summary>
        public int? PrincipalCurrencyId { get; set; }
        public Currency? PrincipalCurrency { get; set; }

        public string? Notes { get; set; }

        /// <summary>Porcentaje de interés; el período lo indica <see cref="InterestRatePeriod"/>.</summary>
        public decimal? InterestPercent { get; set; }

        /// <summary>Periodicidad de <see cref="InterestPercent"/> (interés simple en la estimación).</summary>
        public LoanInterestRatePeriod InterestRatePeriod { get; set; } = LoanInterestRatePeriod.annual;

        /// <summary>Fecha desde la que se aplica el interés (solo fecha, UTC).</summary>
        public DateTime? InterestStartDate { get; set; }

        /// <summary>JSON array de fechas de cobro previstas (ISO 8601 date), opcional.</summary>
        public string? DueDatesJson { get; set; }

        public ICollection<LoanPayment> Payments { get; set; } = new List<LoanPayment>();
    }
}
