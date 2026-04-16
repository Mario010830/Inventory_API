using System;

namespace APICore.Common.DTO.Response
{
    public class LoanPaymentResponse
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
