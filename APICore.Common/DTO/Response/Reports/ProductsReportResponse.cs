using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response.Reports
{
    public class TopSellingProductDto
    {
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public decimal QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal UnitCost { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal TotalReturned { get; set; }
    }

    public class CategoryDistributionDto
    {
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public long ProductsCount { get; set; }
    }

    public class ProductsReportResponse : ReportResponse
    {
        public long TotalProducts { get; set; }
        public long ActiveProducts { get; set; }
        public List<TopSellingProductDto> TopSellingProducts { get; set; } = new();
        public List<CategoryDistributionDto> CategoryDistribution { get; set; } = new();
    }
}

