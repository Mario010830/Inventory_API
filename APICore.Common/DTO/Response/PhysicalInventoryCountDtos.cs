using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    public class PhysicalInventoryCountDetailResponse
    {
        public int Id { get; set; }
        public int DailySummaryId { get; set; }
        public DateTime CountedAt { get; set; }
        public int? UserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<PhysicalInventoryCountItemResponse> Items { get; set; } = new();
    }

    public class PhysicalInventoryCountItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal ExpectedQuantity { get; set; }
        public decimal? CountedQuantity { get; set; }
        public decimal Difference { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal ValuedDifference { get; set; }
    }

    public class PhysicalInventoryCountSummaryResponse
    {
        public int? PhysicalInventoryCountId { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<PhysicalInventoryCountSummaryLineResponse> Lines { get; set; } = new();
        public decimal TotalSurplusValued { get; set; }
        public decimal TotalShortageValued { get; set; }
        public decimal NetValuedImpact { get; set; }
    }

    public class PhysicalInventoryCountSummaryLineResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal ExpectedQuantity { get; set; }
        public decimal? CountedQuantity { get; set; }
        public decimal Difference { get; set; }
        public decimal ValuedDifference { get; set; }
        /// <summary>OK | Sobrante | Faltante</summary>
        public string Classification { get; set; } = string.Empty;
    }
}
