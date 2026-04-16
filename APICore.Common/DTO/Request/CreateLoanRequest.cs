using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Request
{
    public class CreateLoanRequest
    {
        public string DebtorName { get; set; } = null!;
        public decimal PrincipalAmount { get; set; }
        public string? Notes { get; set; }

        /// <summary>Interés anual en % (opcional).</summary>
        public decimal? InterestPercentPerYear { get; set; }

        /// <summary>Fecha desde la que se devenga el interés (opcional).</summary>
        public DateTime? InterestStartDate { get; set; }

        /// <summary>Fechas previstas de cobro (opcional).</summary>
        public IList<DateTime>? DueDates { get; set; }
    }
}
