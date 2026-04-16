using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Request
{
    public class UpdateLoanRequest
    {
        public string? DebtorName { get; set; }
        public decimal? PrincipalAmount { get; set; }

        /// <summary>Moneda del capital. Solo actualiza si se envía un valor entero (omitir para no cambiar).</summary>
        public int? PrincipalCurrencyId { get; set; }

        public string? Notes { get; set; }
        public decimal? InterestPercent { get; set; }

        /// <summary>Periodicidad del porcentaje: daily, weekly, monthly, annual.</summary>
        public string? InterestRatePeriod { get; set; }

        public DateTime? InterestStartDate { get; set; }
        public IList<DateTime>? DueDates { get; set; }
    }
}
