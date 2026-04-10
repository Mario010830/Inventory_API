using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    public class DailySummaryResponseDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int LocationId { get; set; }
        public int OrganizationId { get; set; }
        public decimal OpeningCash { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal TotalOutflows { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal ActualCash { get; set; }
        public decimal Difference { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool IsClosed { get; set; }
        public List<DailySummaryInventoryItemDto> InventoryItems { get; set; } = new();
    }
}
