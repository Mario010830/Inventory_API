using System;

namespace APICore.Data.Entities
{
    /// <summary>Abono o cobro parcial sobre un préstamo.</summary>
    public class LoanPayment : BaseEntity
    {
        public int LoanId { get; set; }
        public Loan Loan { get; set; } = null!;

        public decimal Amount { get; set; }

        /// <summary>Fecha del cobro (UTC).</summary>
        public DateTime PaidAt { get; set; }

        public string? Notes { get; set; }
    }
}
