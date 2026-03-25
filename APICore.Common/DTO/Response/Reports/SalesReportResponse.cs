using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response.Reports
{
    public class SalesByDayDto
    {
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
    }

    public class ReturnsByDayDto
    {
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
    }

    public class SalesOrderRowDto
    {
        public int Id { get; set; }
        public string? Folio { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = null!;
        public int LocationId { get; set; }
        public string? LocationName { get; set; }
        public int? ContactId { get; set; }
        public string? ContactName { get; set; }
        public int ItemsCount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
    }

    public class SalesReportResponse : ReportResponse
    {
        public decimal TotalSales { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal NetSales { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageTicket { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalOrdersCount { get; set; }
        public List<SalesByDayDto> SalesByDay { get; set; } = new();
        public List<ReturnsByDayDto> ReturnsByDay { get; set; } = new();
        public List<SalesOrderRowDto> Orders { get; set; } = new();
    }
}

