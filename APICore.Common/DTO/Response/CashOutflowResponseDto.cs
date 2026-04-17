using System;

namespace APICore.Common.DTO.Response
{
    public class CashOutflowResponseDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int LocationId { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
        public int? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
