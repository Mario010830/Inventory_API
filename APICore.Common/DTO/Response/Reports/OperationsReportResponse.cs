using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response.Reports
{
    public class MovementsByTypeDto
    {
        public string Type { get; set; } = null!;
        public long Count { get; set; }
        public decimal QuantitySum { get; set; }
    }

    public class OperationsMovementRowDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Type { get; set; } = null!;
        public decimal Quantity { get; set; }
        public string? Reason { get; set; }
        public string? ReferenceDocument { get; set; }
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public int LocationId { get; set; }
    }

    public class SupplierSummaryDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = null!;
        public int TotalEntries { get; set; }
        public decimal TotalUnits { get; set; }
    }

    public class OperationsReportResponse : ReportResponse
    {
        public long TotalMovements { get; set; }
        public long Entries { get; set; }
        public long Exits { get; set; }
        public long Adjustments { get; set; }
        public List<MovementsByTypeDto> MovementsByType { get; set; } = new();
        public List<OperationsMovementRowDto> MovementDetails { get; set; } = new();
        public List<SupplierSummaryDto> SupplierSummary { get; set; } = new();
    }
}

