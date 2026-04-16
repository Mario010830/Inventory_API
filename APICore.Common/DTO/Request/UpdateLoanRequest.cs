using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Request
{
    public class UpdateLoanRequest
    {
        public string? DebtorName { get; set; }
        public decimal? PrincipalAmount { get; set; }
        public string? Notes { get; set; }
        public decimal? InterestPercentPerYear { get; set; }
        public DateTime? InterestStartDate { get; set; }
        public IList<DateTime>? DueDates { get; set; }
    }
}
