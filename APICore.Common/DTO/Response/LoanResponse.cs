using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    public class LoanResponse
    {
        public int Id { get; set; }
        public string DebtorName { get; set; } = null!;
        public decimal PrincipalAmount { get; set; }
        public string? Notes { get; set; }
        public decimal? InterestPercent { get; set; }

        /// <summary>Periodicidad del porcentaje: daily, weekly, monthly, annual.</summary>
        public string InterestRatePeriod { get; set; } = "annual";

        public DateTime? InterestStartDate { get; set; }
        public IReadOnlyList<DateTime> DueDates { get; set; } = Array.Empty<DateTime>();

        /// <summary>Suma de cobros registrados.</summary>
        public decimal TotalPaid { get; set; }

        /// <summary>Capital pendiente: principal − total cobrado (no negativo).</summary>
        public decimal OutstandingPrincipal { get; set; }

        /// <summary>Interés estimado sobre el saldo actual (interés simple según InterestRatePeriod desde InterestStartDate).</summary>
        public decimal EstimatedInterest { get; set; }

        /// <summary>Saldo estimado: capital pendiente + interés estimado.</summary>
        public decimal EstimatedTotalDue { get; set; }

        public IReadOnlyList<LoanPaymentResponse> Payments { get; set; } = Array.Empty<LoanPaymentResponse>();
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}
