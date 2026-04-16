using System;

namespace APICore.Common.DTO.Request
{
    public class RegisterLoanPaymentRequest
    {
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public string? Notes { get; set; }
    }
}
