using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APICore.Common.DTO.Response.Reports;
using APICore.Services.Utils;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace APICore.Services.Impls
{
    public partial class ReportsService
    {
        public async Task<SalesSummaryReportResponse> GetSalesSummaryReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            var (from, toExclusive) = NormalizeDateRange(dateFrom, dateTo);

            var ordersQuery = _uow.SaleOrderRepository
                .GetAll()
                .AsNoTracking()
                .Where(o => o.Status == SaleOrderStatus.confirmed);

            if (locationId.HasValue)
                ordersQuery = ordersQuery.Where(o => o.LocationId == locationId.Value);
            if (from.HasValue)
                ordersQuery = ordersQuery.Where(o => o.CreatedAt >= from.Value);
            if (toExclusive.HasValue)
                ordersQuery = ordersQuery.Where(o => o.CreatedAt < toExclusive.Value);

            var returnsQuery = _uow.SaleReturnRepository
                .GetAll()
                .AsNoTracking()
                .Where(r => r.Status == SaleReturnStatus.completed);

            if (locationId.HasValue)
                returnsQuery = returnsQuery.Where(r => r.LocationId == locationId.Value);
            if (from.HasValue)
                returnsQuery = returnsQuery.Where(r => r.CreatedAt >= from.Value);
            if (toExclusive.HasValue)
                returnsQuery = returnsQuery.Where(r => r.CreatedAt < toExclusive.Value);

            var totalOrdersCount = (int)(await ordersQuery.LongCountAsync());
            var totalSales = (await ordersQuery.SumAsync(o => (decimal?)o.Total)) ?? 0m;
            var orderTotalsForDay = await ordersQuery.Select(o => new { o.CreatedAt, o.Total }).ToListAsync();
            var salesByDayRows = orderTotalsForDay
                .GroupBy(o => TimeZoneInfo.ConvertTimeFromUtc(o.CreatedAt, CubaBusinessCalendar.CubaTimeZone).Date)
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Day = g.Key.Day, Total = g.Sum(x => x.Total) })
                .ToList();

            var totalReturns = (await returnsQuery.SumAsync(r => (decimal?)r.Total)) ?? 0m;
            var returnTotalsForDay = await returnsQuery.Select(r => new { r.CreatedAt, r.Total }).ToListAsync();
            var returnsByDayRows = returnTotalsForDay
                .GroupBy(r => TimeZoneInfo.ConvertTimeFromUtc(r.CreatedAt, CubaBusinessCalendar.CubaTimeZone).Date)
                .Select(g => new { Year = g.Key.Year, Month = g.Key.Month, Day = g.Key.Day, Total = g.Sum(x => x.Total) })
                .ToList();

            var salesByDay = salesByDayRows
                .Select(x => new SalesByDayDto
                {
                    Date = new DateTime(x.Year, x.Month, x.Day),
                    Total = x.Total
                })
                .OrderBy(x => x.Date)
                .ToList();

            var returnsByDay = returnsByDayRows
                .Select(x => new ReturnsByDayDto
                {
                    Date = new DateTime(x.Year, x.Month, x.Day),
                    Total = x.Total
                })
                .OrderBy(x => x.Date)
                .ToList();

            var averageTicket = totalOrdersCount > 0 ? totalSales / totalOrdersCount : 0m;

            return new SalesSummaryReportResponse
            {
                TotalSales = totalSales,
                TotalReturns = totalReturns,
                NetSales = totalSales - totalReturns,
                TotalOrders = totalOrdersCount,
                AverageTicket = averageTicket,
                SalesByDay = salesByDay,
                ReturnsByDay = returnsByDay
            };
        }

        public async Task<SalesByProductReportResponse> GetSalesByProductReportAsync(
            DateTime? dateFrom, DateTime? dateTo, int? locationId, int page = 1, int pageSize = 50)
        {
            (page, pageSize) = ClampSalesReportPaging(page, pageSize);
            var baseQ = ConfirmedSaleLineItemsQuery(dateFrom, dateTo, locationId)
                .Where(i => i.Product != null);

            var grouped = baseQ
                .GroupBy(i => new { i.ProductId, ProductCode = i.Product!.Code, ProductName = i.Product.Name })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.ProductCode,
                    g.Key.ProductName,
                    QuantitySold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.LineTotal),
                    LineDiscounts = g.Sum(x => x.Discount),
                    OrdersCount = g.Select(x => x.SaleOrderId).Distinct().Count()
                });

            var totalCount = await grouped.CountAsync();
            var rows = await grouped
                .OrderByDescending(x => x.Revenue)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new SalesByProductReportResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = rows.Select(x => new SalesByProductRowDto
                {
                    ProductId = x.ProductId,
                    ProductCode = x.ProductCode,
                    ProductName = x.ProductName,
                    QuantitySold = x.QuantitySold,
                    Revenue = x.Revenue,
                    LineDiscounts = x.LineDiscounts,
                    OrdersCount = x.OrdersCount
                }).ToList()
            };
        }

        public async Task<SalesByCategoryReportResponse> GetSalesByCategoryReportAsync(
            DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            var lines = ConfirmedSaleLineItemsQuery(dateFrom, dateTo, locationId)
                .Where(i => i.Product != null);
            var products = _uow.ProductRepository.GetAllIncluding(p => p.Category!).AsNoTracking();

            var rows = await (
                    from i in lines
                    join p in products on i.ProductId equals p.Id
                    group i by new
                    {
                        p.CategoryId,
                        CategoryName = p.Category != null ? p.Category.Name : "Sin categoría"
                    }
                    into g
                    select new SalesByCategoryRowDto
                    {
                        CategoryId = g.Key.CategoryId,
                        CategoryName = g.Key.CategoryName,
                        QuantitySold = g.Sum(x => x.Quantity),
                        Revenue = g.Sum(x => x.LineTotal),
                        OrdersCount = g.Select(x => x.SaleOrderId).Distinct().Count()
                    })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            return new SalesByCategoryReportResponse { Items = rows };
        }

        public async Task<SalesByEmployeeReportResponse> GetSalesByEmployeeReportAsync(
            DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            var (from, toExclusive) = NormalizeDateRange(dateFrom, dateTo);
            var ordersBase = _uow.SaleOrderRepository.GetAll().AsNoTracking()
                .Where(o => o.Status == SaleOrderStatus.confirmed);

            if (locationId.HasValue)
                ordersBase = ordersBase.Where(o => o.LocationId == locationId.Value);
            if (from.HasValue)
                ordersBase = ordersBase.Where(o => o.CreatedAt >= from.Value);
            if (toExclusive.HasValue)
                ordersBase = ordersBase.Where(o => o.CreatedAt < toExclusive.Value);

            var users = _uow.UserRepository.GetAll().AsNoTracking();

            var rows = await (
                    from o in ordersBase
                    join u in users on o.UserId equals u.Id into uj
                    from u in uj.DefaultIfEmpty()
                    group o by new { o.UserId, FullName = u != null ? u.FullName : null }
                    into g
                    select new
                    {
                        g.Key.UserId,
                        g.Key.FullName,
                        OrdersCount = g.Count(),
                        TotalSales = g.Sum(x => x.Total)
                    })
                .OrderByDescending(x => x.TotalSales)
                .ToListAsync();

            return new SalesByEmployeeReportResponse
            {
                Items = rows.Select(x => new SalesByEmployeeRowDto
                {
                    UserId = x.UserId,
                    UserFullName = string.IsNullOrWhiteSpace(x.FullName) ? "Sin asignar" : x.FullName,
                    OrdersCount = x.OrdersCount,
                    TotalSales = x.TotalSales,
                    AverageTicket = x.OrdersCount > 0 ? x.TotalSales / x.OrdersCount : 0m
                }).ToList()
            };
        }

        public async Task<SalesByPaymentReportResponse> GetSalesByPaymentReportAsync(
            DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            var (from, toExclusive) = NormalizeDateRange(dateFrom, dateTo);
            var pay = _uow.SaleOrderPaymentRepository
                .GetAllIncluding(p => p.SaleOrder!, p => p.PaymentMethod!)
                .AsNoTracking()
                .Where(p => p.SaleOrder != null && p.SaleOrder.Status == SaleOrderStatus.confirmed);

            if (locationId.HasValue)
                pay = pay.Where(p => p.SaleOrder!.LocationId == locationId.Value);
            if (from.HasValue)
                pay = pay.Where(p => p.SaleOrder!.CreatedAt >= from.Value);
            if (toExclusive.HasValue)
                pay = pay.Where(p => p.SaleOrder!.CreatedAt < toExclusive.Value);

            var rows = await pay
                .GroupBy(p => new
                {
                    p.PaymentMethodId,
                    Name = p.PaymentMethod != null ? p.PaymentMethod.Name : "?",
                    Ref = p.PaymentMethod != null ? p.PaymentMethod.InstrumentReference : null
                })
                .Select(g => new SalesByPaymentRowDto
                {
                    PaymentMethodId = g.Key.PaymentMethodId,
                    PaymentMethodName = g.Key.Name,
                    InstrumentReference = g.Key.Ref,
                    TotalAmount = g.Sum(x => x.Amount),
                    PaymentsCount = g.Count()
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToListAsync();

            return new SalesByPaymentReportResponse { Items = rows };
        }

        public async Task<ReceiptsReportResponse> GetReceiptsReportAsync(
            DateTime? dateFrom, DateTime? dateTo, int? locationId, string? folioContains, int page = 1, int pageSize = 50)
        {
            (page, pageSize) = ClampSalesReportPaging(page, pageSize);
            var orders = BuildConfirmedSaleOrdersDetailedQuery(dateFrom, dateTo, locationId);
            if (!string.IsNullOrWhiteSpace(folioContains))
            {
                var f = folioContains.Trim();
                orders = orders.Where(o => o.Folio != null && o.Folio.Contains(f));
            }

            var totalCount = await orders.CountAsync();
            var users = _uow.UserRepository.GetAll().AsNoTracking();

            var pageRows = await (
                    from o in orders
                    join u in users on o.UserId equals u.Id into uj
                    from u in uj.DefaultIfEmpty()
                    orderby o.CreatedAt descending
                    select new ReceiptRowDto
                    {
                        Id = o.Id,
                        Folio = o.Folio,
                        CreatedAt = o.CreatedAt,
                        Total = o.Total,
                        LocationId = o.LocationId,
                        LocationName = o.Location != null ? o.Location.Name : null,
                        UserId = o.UserId,
                        UserFullName = u != null ? u.FullName : null,
                        ContactId = o.ContactId,
                        ContactName = o.Contact != null ? o.Contact.Name : null,
                        ItemsCount = o.Items.Count,
                        Subtotal = o.Subtotal,
                        DiscountAmount = o.DiscountAmount
                    })
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new ReceiptsReportResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = pageRows
            };
        }

        public Task<SalesByModifierReportResponse> GetSalesByModifierReportAsync(
            DateTime? dateFrom, DateTime? dateTo, int? locationId) =>
            Task.FromResult(new SalesByModifierReportResponse());

        public async Task<SalesDiscountsReportResponse> GetSalesDiscountsReportAsync(
            DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            var (from, toExclusive) = NormalizeDateRange(dateFrom, dateTo);
            var ordersQ = _uow.SaleOrderRepository.GetAll().AsNoTracking()
                .Where(o => o.Status == SaleOrderStatus.confirmed);
            if (locationId.HasValue)
                ordersQ = ordersQ.Where(o => o.LocationId == locationId.Value);
            if (from.HasValue)
                ordersQ = ordersQ.Where(o => o.CreatedAt >= from.Value);
            if (toExclusive.HasValue)
                ordersQ = ordersQ.Where(o => o.CreatedAt < toExclusive.Value);

            var totalOrderDiscounts = (await ordersQ.SumAsync(o => (decimal?)o.DiscountAmount)) ?? 0m;

            var lines = ConfirmedSaleLineItemsQuery(dateFrom, dateTo, locationId);
            var totalLineDiscounts = (await lines.SumAsync(i => (decimal?)i.Discount)) ?? 0m;

            var promoLines = lines.Where(i => i.PromotionId.HasValue);
            var promos = _uow.PromotionRepository.GetAllIncluding(pr => pr.Product!).AsNoTracking();

            var byPromotion = await (
                    from i in promoLines
                    join pr in promos on i.PromotionId equals pr.Id
                    group i by new { pr.Id, pr.Type, ProductName = pr.Product != null ? pr.Product.Name : null }
                    into g
                    select new SalesPromotionDiscountRowDto
                    {
                        PromotionId = g.Key.Id,
                        ProductName = g.Key.ProductName,
                        PromotionType = g.Key.Type.ToString(),
                        LinesCount = g.Count(),
                        LineDiscountsSum = g.Sum(x => x.Discount)
                    })
                .OrderByDescending(x => x.LineDiscountsSum)
                .ToListAsync();

            return new SalesDiscountsReportResponse
            {
                TotalOrderDiscounts = totalOrderDiscounts,
                TotalLineDiscounts = totalLineDiscounts,
                TotalDiscounts = totalOrderDiscounts + totalLineDiscounts,
                ByPromotion = byPromotion
            };
        }

        public Task<SalesTaxesReportResponse> GetSalesTaxesReportAsync(
            DateTime? dateFrom, DateTime? dateTo, int? locationId) =>
            Task.FromResult(new SalesTaxesReportResponse());

        public async Task<CashRegisterReportResponse> GetCashRegisterReportAsync(
            DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            var (from, toExclusive) = NormalizeDateRange(dateFrom, dateTo);
            var outflows = _uow.CashOutflowRepository.GetAll().AsNoTracking();
            if (locationId.HasValue)
                outflows = outflows.Where(c => c.LocationId == locationId.Value);
            if (from.HasValue)
                outflows = outflows.Where(c => c.Date >= from.Value);
            if (toExclusive.HasValue)
                outflows = outflows.Where(c => c.Date < toExclusive.Value);

            var totalOut = (await outflows.SumAsync(c => (decimal?)c.Amount)) ?? 0m;
            var countOut = await outflows.CountAsync();

            var paymentsReport = await GetSalesByPaymentReportAsync(dateFrom, dateTo, locationId);

            return new CashRegisterReportResponse
            {
                TotalCashOutflows = totalOut,
                CashOutflowsCount = countOut,
                PaymentsByMethod = paymentsReport.Items
            };
        }
    }
}
