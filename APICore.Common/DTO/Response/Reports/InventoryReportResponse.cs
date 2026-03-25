using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response.Reports
{
    public class LowStockProductDto
    {
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public decimal TotalStock { get; set; }
    }

    public class StockByProductDto
    {
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public decimal TotalStock { get; set; }
    }

    public class MovementsSummaryDto
    {
        public long TotalMovements { get; set; }
        public long Entries { get; set; }
        public long Exits { get; set; }
        public long Adjustments { get; set; }
    }

    public class InventoryMovementRowDto
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

    public class InventoryMovementByTypeDto
    {
        public string Type { get; set; } = null!;
        public long Count { get; set; }
        public decimal QuantitySum { get; set; }
    }

    public class InventoryReportResponse : ReportResponse
    {
        public decimal TotalStock { get; set; }
        public List<LowStockProductDto> LowStockProducts { get; set; } = new();
        public decimal InventoryValue { get; set; }
        public List<StockByProductDto> StockByProduct { get; set; } = new();
        public MovementsSummaryDto MovementsSummary { get; set; } = new();
        public List<InventoryMovementRowDto> MovementDetails { get; set; } = new();
    }
}

