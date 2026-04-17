using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response.Reports
{
    /// <summary>Resumen de ventas (totales y series diarias, sin listado de pedidos).</summary>
    public class SalesSummaryReportResponse : ReportResponse
    {
        public decimal TotalSales { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal NetSales { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageTicket { get; set; }
        public List<SalesByDayDto> SalesByDay { get; set; } = new();
        public List<ReturnsByDayDto> ReturnsByDay { get; set; } = new();
    }

    public class SalesByProductRowDto
    {
        public int ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public decimal QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal LineDiscounts { get; set; }
        public int OrdersCount { get; set; }
    }

    public class SalesByProductReportResponse : ReportResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<SalesByProductRowDto> Items { get; set; } = new();
    }

    public class SalesByCategoryRowDto
    {
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public decimal QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public int OrdersCount { get; set; }
    }

    public class SalesByCategoryReportResponse : ReportResponse
    {
        public List<SalesByCategoryRowDto> Items { get; set; } = new();
    }

    public class SalesByEmployeeRowDto
    {
        public int? UserId { get; set; }
        public string? UserFullName { get; set; }
        public int OrdersCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal AverageTicket { get; set; }
    }

    public class SalesByEmployeeReportResponse : ReportResponse
    {
        public List<SalesByEmployeeRowDto> Items { get; set; } = new();
    }

    public class SalesByPaymentRowDto
    {
        public int PaymentMethodId { get; set; }
        public string PaymentMethodName { get; set; } = null!;
        public string? InstrumentReference { get; set; }
        public decimal TotalAmount { get; set; }
        public int PaymentsCount { get; set; }
    }

    public class SalesByPaymentReportResponse : ReportResponse
    {
        public List<SalesByPaymentRowDto> Items { get; set; } = new();
    }

    public class ReceiptRowDto
    {
        public int Id { get; set; }
        public string? Folio { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Total { get; set; }
        public int LocationId { get; set; }
        public string? LocationName { get; set; }
        public int? UserId { get; set; }
        public string? UserFullName { get; set; }
        public int? ContactId { get; set; }
        public string? ContactName { get; set; }
        public int ItemsCount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
    }

    public class ReceiptsReportResponse : ReportResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<ReceiptRowDto> Items { get; set; } = new();
    }

    public class SalesByModifierReportResponse : ReportResponse
    {
        public bool NotModeled { get; set; } = true;
        public string Message { get; set; } =
            "Este dominio no incluye modificadores de artículo; el informe no está disponible.";
        public List<object> Items { get; set; } = new();
    }

    public class SalesDiscountsReportResponse : ReportResponse
    {
        public decimal TotalOrderDiscounts { get; set; }
        public decimal TotalLineDiscounts { get; set; }
        public decimal TotalDiscounts { get; set; }
        public List<SalesPromotionDiscountRowDto> ByPromotion { get; set; } = new();
    }

    public class SalesPromotionDiscountRowDto
    {
        public int PromotionId { get; set; }
        public string? ProductName { get; set; }
        public string PromotionType { get; set; } = null!;
        public int LinesCount { get; set; }
        public decimal LineDiscountsSum { get; set; }
    }

    public class SalesTaxesReportResponse : ReportResponse
    {
        public bool NotModeled { get; set; } = true;
        public string Message { get; set; } =
            "Las ventas no registran impuestos por línea ni por ticket en este modelo; el informe no está disponible.";
        public List<object> Items { get; set; } = new();
    }

    public class CashRegisterReportResponse : ReportResponse
    {
        public decimal TotalCashOutflows { get; set; }
        public int CashOutflowsCount { get; set; }
        public List<SalesByPaymentRowDto> PaymentsByMethod { get; set; } = new();
    }
}
