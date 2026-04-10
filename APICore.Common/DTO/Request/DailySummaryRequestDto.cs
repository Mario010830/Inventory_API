using System;

namespace APICore.Common.DTO.Request
{
    public class DailySummaryRequestDto
    {
        public DateTime Date { get; set; }
        public decimal OpeningCash { get; set; }
        public decimal ActualCash { get; set; }
        public string? Notes { get; set; }
    }
}
