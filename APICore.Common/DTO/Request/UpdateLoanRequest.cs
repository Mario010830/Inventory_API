using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Request
{
    public class UpdateLoanRequest
    {
        public string? DebtorName { get; set; }
        public decimal? PrincipalAmount { get; set; }
        public string? Notes { get; set; }
        public decimal? InterestPercent { get; set; }

        /// <summary>Periodicidad del porcentaje: daily, weekly, monthly, annual.</summary>
        public string? InterestRatePeriod { get; set; }

        public DateTime? InterestStartDate { get; set; }
        public IList<DateTime>? DueDates { get; set; }
    }
}
