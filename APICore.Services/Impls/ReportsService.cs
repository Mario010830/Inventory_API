using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APICore.Common.DTO.Response.Reports;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Data.UoW;
using APICore.Services;
using APICore.Services.Utils;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace APICore.Services.Impls
{
    public partial class ReportsService : IReportsService
    {
        private sealed class SalesOrderExportRow
        {
            public int Id { get; set; }
            public string? Folio { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal Total { get; set; }
            public string Status { get; set; } = "";
            public int LocationId { get; set; }
            public string? LocationName { get; set; }
            public int? ContactId { get; set; }
            public string? ContactName { get; set; }
            public int ItemsCount { get; set; }
            public decimal Subtotal { get; set; }
            public decimal DiscountAmount { get; set; }
        }

        private readonly IUnitOfWork _uow;
        private readonly IInventorySettings _inventorySettings;

        public ReportsService(IUnitOfWork uow, IInventorySettings inventorySettings)
        {
            _uow = uow;
            _inventorySettings = inventorySettings ?? throw new ArgumentNullException(nameof(inventorySettings));
        }

        private static (DateTime? From, DateTime? ToExclusive) NormalizeDateRange(DateTime? dateFrom, DateTime? dateTo)
        {
            if (dateFrom == null && dateTo == null)
                return (null, null);

            DateTime? fromUtc = null;
            DateTime? toExclusiveUtc = null;
            if (dateFrom.HasValue)
            {
                var d = dateFrom.Value.Date;
                fromUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(d, DateTimeKind.Unspecified), CubaBusinessCalendar.CubaTimeZone);
            }

            if (dateTo.HasValue)
            {
                var d = dateTo.Value.Date.AddDays(1);
                toExclusiveUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(d, DateTimeKind.Unspecified), CubaBusinessCalendar.CubaTimeZone);
            }

            if (fromUtc.HasValue && toExclusiveUtc.HasValue && fromUtc.Value >= toExclusiveUtc.Value)
                return (null, null);

            return (fromUtc, toExclusiveUtc);
        }

        private IQueryable<SaleOrder> BuildConfirmedSaleOrdersDetailedQuery(DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            var (from, toExclusive) = NormalizeDateRange(dateFrom, dateTo);

            var query = _uow.SaleOrderRepository
                .GetAllIncluding(o => o.Location, o => o.Contact)
                .AsNoTracking()
                .Where(o => o.Status == SaleOrderStatus.confirmed);

            if (locationId.HasValue)
                query = query.Where(o => o.LocationId == locationId.Value);
            if (from.HasValue)
                query = query.Where(o => o.CreatedAt >= from.Value);
            if (toExclusive.HasValue)
                query = query.Where(o => o.CreatedAt < toExclusive.Value);

            return query;
        }

        private IQueryable<SaleOrderItem> ConfirmedSaleLineItemsQuery(DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            var (from, toExclusive) = NormalizeDateRange(dateFrom, dateTo);
            var q = _uow.SaleOrderItemRepository
                .GetAllIncluding(i => i.SaleOrder!, i => i.Product!)
                .AsNoTracking()
                .Where(i => i.SaleOrder != null && i.SaleOrder.Status == SaleOrderStatus.confirmed);

            if (locationId.HasValue)
                q = q.Where(i => i.SaleOrder!.LocationId == locationId.Value);
            if (from.HasValue)
                q = q.Where(i => i.SaleOrder!.CreatedAt >= from.Value);
            if (toExclusive.HasValue)
                q = q.Where(i => i.SaleOrder!.CreatedAt < toExclusive.Value);

            return q;
        }

        private static (int Page, int PageSize) ClampSalesReportPaging(int page, int pageSize)
        {
            if (pageSize > 100)
                pageSize = 100;
            if (page < 1)
                page = 1;
            return (page, pageSize);
        }

        private async Task<List<SalesOrderExportRow>> LoadSalesOrdersExportRowsAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            return await BuildConfirmedSaleOrdersDetailedQuery(dateFrom, dateTo, locationId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new SalesOrderExportRow
                {
                    Id = o.Id,
                    Folio = o.Folio,
                    CreatedAt = o.CreatedAt,
                    Total = o.Total,
                    Status = o.Status.ToString(),
                    LocationId = o.LocationId,
                    LocationName = o.Location != null ? o.Location.Name : null,
                    ContactId = o.ContactId,
                    ContactName = o.Contact != null ? o.Contact.Name : null,
                    ItemsCount = o.Items.Count,
                    Subtotal = o.Subtotal,
                    DiscountAmount = o.DiscountAmount
                })
                .ToListAsync();
        }

        private static string CsvEscape(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(',') || value.Contains('\n') || value.Contains('\r') || value.Contains('"'))
                return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

            return value;
        }

        public async Task<byte[]> ExportSalesOrdersCsvAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            var rows = await LoadSalesOrdersExportRowsAsync(dateFrom, dateTo, locationId);

            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",",
                "Id", "Folio", "CreatedAt", "Total", "Status", "LocationId", "LocationName",
                "ContactId", "ContactName", "ItemsCount", "Subtotal", "DiscountAmount"));

            foreach (var o in rows)
            {
                var line = string.Join(",",
                    o.Id.ToString(inv),
                    CsvEscape(o.Folio),
                    CsvEscape(o.CreatedAt.ToString("o", inv)),
                    o.Total.ToString(inv),
                    CsvEscape(o.Status),
                    o.LocationId.ToString(inv),
                    CsvEscape(o.LocationName),
                    o.ContactId?.ToString(inv) ?? "",
                    CsvEscape(o.ContactName),
                    o.ItemsCount.ToString(inv),
                    o.Subtotal.ToString(inv),
                    o.DiscountAmount.ToString(inv));
                sb.AppendLine(line);
            }

            return new UTF8Encoding(true).GetBytes(sb.ToString());
        }

        public async Task<byte[]> ExportSalesOrdersPdfAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            return await ExportSalesOrdersPdfInternalAsync(dateFrom, dateTo, locationId);
        }

        private async Task<byte[]> ExportSalesOrdersPdfInternalAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            var rows = await LoadSalesOrdersExportRowsAsync(dateFrom, dateTo, locationId);
            var inv = CultureInfo.InvariantCulture;

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(24);
                    page.DefaultTextStyle(x => x.FontSize(7));

                    page.Header().PaddingBottom(8).Column(col =>
                    {
                        col.Item().Text("Reporte de pedidos (ventas confirmadas)").SemiBold().FontSize(12);
                        col.Item().Text($"Generado (UTC): {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", inv)}")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Medium);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(36);
                            c.RelativeColumn(1f);
                            c.RelativeColumn(1.1f);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(0.7f);
                            c.ConstantColumn(36);
                            c.RelativeColumn(1f);
                            c.ConstantColumn(36);
                            c.RelativeColumn(1f);
                            c.ConstantColumn(32);
                            c.RelativeColumn(0.9f);
                            c.RelativeColumn(0.9f);
                        });

                        table.Header(header =>
                        {
                            void H(string label) =>
                                header.Cell().Padding(3).Background(Colors.Grey.Lighten3).BorderBottom(1)
                                    .BorderColor(Colors.Grey.Medium).Text(label).SemiBold();

                            H("Id");
                            H("Folio");
                            H("Fecha");
                            H("Total");
                            H("Estado");
                            H("Loc.");
                            H("Ubicación");
                            H("Cont.");
                            H("Contacto");
                            H("Ítems");
                            H("Subtotal");
                            H("Desc.");
                        });

                        foreach (var o in rows)
                        {
                            void C(string text) =>
                                table.Cell().Padding(3).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Text(text);

                            C(o.Id.ToString(inv));
                            C(o.Folio ?? "");
                            C(o.CreatedAt.ToString("yyyy-MM-dd HH:mm", inv));
                            C(o.Total.ToString("N2", inv));
                            C(o.Status);
                            C(o.LocationId.ToString(inv));
                            C(o.LocationName ?? "");
                            C(o.ContactId?.ToString(inv) ?? "");
                            C(o.ContactName ?? "");
                            C(o.ItemsCount.ToString(inv));
                            C(o.Subtotal.ToString("N2", inv));
                            C(o.DiscountAmount.ToString("N2", inv));
                        }
                    });
                });
            });

            return pdf.GeneratePdf();
        }

        public async Task<SalesReportResponse> GetSalesReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId, int page = 1, int pageSize = 50)
        {
            // IMPORTANT: evitar operaciones EF en paralelo sobre el mismo DbContext.
            if (pageSize > 100)
                pageSize = 100;
            if (page < 1)
                page = 1;

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

            // Agregados: ejecutar secuencialmente para no romper DbContext.
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

            var ordersListQuery = BuildConfirmedSaleOrdersDetailedQuery(dateFrom, dateTo, locationId);
            var ordersListRows = await ordersListQuery
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new
                {
                    o.Id,
                    o.Folio,
                    o.CreatedAt,
                    o.Total,
                    Status = o.Status,
                    o.LocationId,
                    LocationName = o.Location != null ? o.Location.Name : null,
                    o.ContactId,
                    ContactName = o.Contact != null ? o.Contact.Name : null,
                    ItemsCount = o.Items.Count,
                    o.Subtotal,
                    o.DiscountAmount
                })
                .ToListAsync();

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

            var orders = ordersListRows
                .Select(o => new SalesOrderRowDto
                {
                    Id = o.Id,
                    Folio = o.Folio,
                    CreatedAt = o.CreatedAt,
                    Total = o.Total,
                    Status = o.Status.ToString(),
                    LocationId = o.LocationId,
                    LocationName = o.LocationName,
                    ContactId = o.ContactId,
                    ContactName = o.ContactName,
                    ItemsCount = o.ItemsCount,
                    Subtotal = o.Subtotal,
                    DiscountAmount = o.DiscountAmount
                })
                .ToList();

            var averageTicket = totalOrdersCount > 0
                ? totalSales / totalOrdersCount
                : 0m;

            return new SalesReportResponse
            {
                TotalSales = totalSales,
                TotalReturns = totalReturns,
                NetSales = totalSales - totalReturns,
                TotalOrders = totalOrdersCount,
                AverageTicket = averageTicket,
                Page = page,
                PageSize = pageSize,
                TotalOrdersCount = totalOrdersCount,
                SalesByDay = salesByDay,
                ReturnsByDay = returnsByDay,
                Orders = orders
            };
        }

        public async Task<InventoryReportResponse> GetInventoryReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            // IMPORTANT: evitar operaciones EF en paralelo sobre el mismo DbContext.
            var minStock = _inventorySettings.DefaultMinimumStock;

            // Stock actual (sin depender del rango de fechas).
            var inventoryWithProductQuery = _uow.InventoryRepository
                .GetAllIncluding(i => i.Product)
                .AsNoTracking();

            var inventoryQuery = inventoryWithProductQuery;
            if (locationId.HasValue)
                inventoryQuery = inventoryQuery.Where(i => i.LocationId == locationId.Value);

            var totalStock = (await inventoryQuery.SumAsync(i => (decimal?)i.CurrentStock)) ?? 0m;
            var inventoryValue = (await inventoryQuery
                .SumAsync(i => (decimal?)(i.CurrentStock * (i.Product != null ? i.Product.Costo : 0)))) ?? 0m;

            var stockByProductRows = await inventoryQuery
                .GroupBy(inv => new
                {
                    inv.ProductId,
                    ProductCode = inv.Product.Code,
                    ProductName = inv.Product.Name
                })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    ProductCode = g.Key.ProductCode,
                    ProductName = g.Key.ProductName,
                    TotalStock = g.Sum(x => x.CurrentStock)
                })
                .OrderByDescending(x => x.TotalStock)
                .Take(50)
                .ToListAsync();

            var lowStockProductsRows = await inventoryQuery
                .GroupBy(inv => new
                {
                    inv.ProductId,
                    ProductCode = inv.Product.Code,
                    ProductName = inv.Product.Name
                })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    ProductCode = g.Key.ProductCode,
                    ProductName = g.Key.ProductName,
                    TotalStock = g.Sum(x => x.CurrentStock)
                })
                .Where(x => x.TotalStock <= minStock)
                .OrderBy(x => x.TotalStock)
                .Take(20)
                .ToListAsync();

            // Movimientos en el rango de fechas.
            var (from, toExclusive) = NormalizeDateRange(dateFrom, dateTo);

            var movementQuery = _uow.InventoryMovementRepository.GetAll().AsNoTracking();
            if (locationId.HasValue)
                movementQuery = movementQuery.Where(m => m.LocationId == locationId.Value);
            if (from.HasValue)
                movementQuery = movementQuery.Where(m => m.CreatedAt >= from.Value);
            if (toExclusive.HasValue)
                movementQuery = movementQuery.Where(m => m.CreatedAt < toExclusive.Value);

            var totalMovements = await movementQuery.LongCountAsync();
            var entries = await movementQuery.LongCountAsync(m => m.Type == InventoryMovementType.entry);
            var exits = await movementQuery.LongCountAsync(m => m.Type == InventoryMovementType.exit);
            var adjustments = await movementQuery.LongCountAsync(m => m.Type == InventoryMovementType.adjustment);

            var movementDetailQuery = _uow.InventoryMovementRepository
                .GetAllIncluding(m => m.Product, m => m.Supplier)
                .AsNoTracking();

            if (locationId.HasValue)
                movementDetailQuery = movementDetailQuery.Where(m => m.LocationId == locationId.Value);
            if (from.HasValue)
                movementDetailQuery = movementDetailQuery.Where(m => m.CreatedAt >= from.Value);
            if (toExclusive.HasValue)
                movementDetailQuery = movementDetailQuery.Where(m => m.CreatedAt < toExclusive.Value);

            var movementDetails = await movementDetailQuery
                .OrderByDescending(m => m.CreatedAt)
                .Take(100)
                .Select(m => new InventoryMovementRowDto
                {
                    Id = m.Id,
                    CreatedAt = m.CreatedAt,
                    Type = m.Type.ToString(),
                    Quantity = m.Quantity,
                    Reason = m.Reason,
                    ReferenceDocument = m.ReferenceDocument,
                    ProductId = m.ProductId,
                    ProductCode = m.Product != null ? m.Product.Code : null,
                    ProductName = m.Product != null ? m.Product.Name : null,
                    SupplierId = m.SupplierId,
                    SupplierName = m.Supplier != null ? m.Supplier.Name : null,
                    LocationId = m.LocationId
                })
                .ToListAsync();

            return new InventoryReportResponse
            {
                TotalStock = totalStock,
                LowStockProducts = lowStockProductsRows
                    .Select(x => new LowStockProductDto
                    {
                        ProductId = x.ProductId,
                        ProductCode = x.ProductCode,
                        ProductName = x.ProductName,
                        TotalStock = x.TotalStock
                    }).ToList(),
                InventoryValue = inventoryValue,
                StockByProduct = stockByProductRows
                    .Select(x => new StockByProductDto
                    {
                        ProductId = x.ProductId,
                        ProductCode = x.ProductCode,
                        ProductName = x.ProductName,
                        TotalStock = x.TotalStock
                    }).ToList(),
                MovementsSummary = new MovementsSummaryDto
                {
                    TotalMovements = totalMovements,
                    Entries = entries,
                    Exits = exits,
                    Adjustments = adjustments
                },
                MovementDetails = movementDetails
            };
        }

        public async Task<ProductsReportResponse> GetProductsReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            // IMPORTANT: evitar operaciones EF en paralelo sobre el mismo DbContext.
            var (from, toExclusive) = NormalizeDateRange(dateFrom, dateTo);

            var productsQuery = _uow.ProductRepository.GetAll().AsNoTracking().Where(p => !p.IsDeleted);
            var totalProducts = await productsQuery.LongCountAsync();
            var activeProducts = await productsQuery.LongCountAsync(p => p.IsAvailable && p.IsForSale);

            var soldItemsQuery = _uow.SaleOrderItemRepository
                .GetAllIncluding(i => i.Product, i => i.SaleOrder)
                .AsNoTracking()
                .Where(i => i.SaleOrder != null && i.SaleOrder.Status == SaleOrderStatus.confirmed);

            if (locationId.HasValue)
                soldItemsQuery = soldItemsQuery.Where(i => i.SaleOrder!.LocationId == locationId.Value);
            if (from.HasValue)
                soldItemsQuery = soldItemsQuery.Where(i => i.SaleOrder!.CreatedAt >= from.Value);
            if (toExclusive.HasValue)
                soldItemsQuery = soldItemsQuery.Where(i => i.SaleOrder!.CreatedAt < toExclusive.Value);

            var topSellingRows = await soldItemsQuery
                .GroupBy(i => new
                {
                    i.ProductId,
                    ProductCode = i.Product != null ? i.Product.Code : null,
                    ProductName = i.Product != null ? i.Product.Name : null,
                    UnitCost = i.Product != null ? i.Product.Costo : 0m
                })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    ProductCode = g.Key.ProductCode,
                    ProductName = g.Key.ProductName,
                    UnitCost = g.Key.UnitCost,
                    QuantitySold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.LineTotal),
                    AveragePrice = g.Average(x => x.UnitPrice)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(10)
                .ToListAsync();

            var returnedItemsQuery = _uow.SaleReturnItemRepository
                .GetAllIncluding(i => i.SaleReturn)
                .AsNoTracking()
                .Where(i => i.SaleReturn != null && i.SaleReturn.Status == SaleReturnStatus.completed);

            if (locationId.HasValue)
                returnedItemsQuery = returnedItemsQuery.Where(i => i.SaleReturn!.LocationId == locationId.Value);
            if (from.HasValue)
                returnedItemsQuery = returnedItemsQuery.Where(i => i.SaleReturn!.CreatedAt >= from.Value);
            if (toExclusive.HasValue)
                returnedItemsQuery = returnedItemsQuery.Where(i => i.SaleReturn!.CreatedAt < toExclusive.Value);

            var returnedByProductRows = await returnedItemsQuery
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, TotalReturned = g.Sum(x => x.Quantity) })
                .ToListAsync();

            var returnedDict = returnedByProductRows.ToDictionary(x => x.ProductId, x => x.TotalReturned);

            var productsWithCategoryQuery = _uow.ProductRepository
                .GetAllIncluding(p => p.Category)
                .AsNoTracking();

            var categoryDistributionRows = await productsWithCategoryQuery
                .Where(p => p.Category != null)
                .GroupBy(p => new { CategoryId = p.CategoryId, CategoryName = p.Category!.Name })
                .Select(g => new
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    ProductsCount = (long)g.LongCount()
                })
                .OrderByDescending(x => x.ProductsCount)
                .ThenBy(x => x.CategoryName)
                .Take(20)
                .ToListAsync();

            return new ProductsReportResponse
            {
                TotalProducts = totalProducts,
                ActiveProducts = activeProducts,
                TopSellingProducts = topSellingRows.Select(x => new TopSellingProductDto
                {
                    ProductId = x.ProductId,
                    ProductCode = x.ProductCode,
                    ProductName = x.ProductName,
                    QuantitySold = x.QuantitySold,
                    Revenue = x.Revenue,
                    UnitCost = x.UnitCost,
                    AveragePrice = x.AveragePrice,
                    TotalReturned = returnedDict.TryGetValue(x.ProductId, out var ret) ? ret : 0m
                }).ToList(),
                CategoryDistribution = categoryDistributionRows.Select(x => new CategoryDistributionDto
                {
                    CategoryId = x.CategoryId,
                    CategoryName = x.CategoryName,
                    ProductsCount = x.ProductsCount
                }).ToList()
            };
        }

        public async Task<CrmReportResponse> GetCrmReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            // IMPORTANT: evitar operaciones EF en paralelo sobre el mismo DbContext.
            var (from, toExclusive) = NormalizeDateRange(dateFrom, dateTo);

            // Nota: los Leads/Contacts dependen del tenant por OrganizationId.
            var leadsQuery = _uow.LeadRepository.GetAll().AsNoTracking();
            var leadsWithContactQuery = _uow.LeadRepository
                .GetAllIncluding(l => l.ConvertedToContact)
                .AsNoTracking();

            if (from.HasValue)
                leadsQuery = leadsQuery.Where(l => l.CreatedAt >= from.Value);
            if (toExclusive.HasValue)
                leadsQuery = leadsQuery.Where(l => l.CreatedAt < toExclusive.Value);

            var totalLeads = await leadsQuery.LongCountAsync();
            var convertedLeads = await leadsQuery.LongCountAsync(l => l.ConvertedToContactId.HasValue);
            var conversionRate = totalLeads > 0 ? (decimal)convertedLeads / totalLeads : 0m;

            var leadsListQuery = leadsWithContactQuery;
            if (from.HasValue)
                leadsListQuery = leadsListQuery.Where(l => l.CreatedAt >= from.Value);
            if (toExclusive.HasValue)
                leadsListQuery = leadsListQuery.Where(l => l.CreatedAt < toExclusive.Value);

            var leadsListRows = await leadsListQuery
                .OrderByDescending(l => l.CreatedAt)
                .Take(50)
                .Select(l => new
                {
                    LeadId = l.Id,
                    l.Name,
                    l.Company,
                    l.Status,
                    l.CreatedAt,
                    l.ConvertedToContactId,
                    l.ConvertedAt,
                    ContactName = l.ConvertedToContact != null ? l.ConvertedToContact.Name : null
                })
                .ToListAsync();

            return new CrmReportResponse
            {
                TotalLeads = totalLeads,
                ConvertedLeads = convertedLeads,
                ConversionRate = conversionRate,
                Leads = leadsListRows.Select(x => new LeadRowDto
                {
                    LeadId = x.LeadId,
                    Name = x.Name,
                    Company = x.Company,
                    Status = x.Status,
                    CreatedAt = x.CreatedAt,
                    ConvertedToContactId = x.ConvertedToContactId,
                    ConvertedAt = x.ConvertedAt,
                    ContactName = x.ContactName
                }).ToList()
            };
        }

        public async Task<OperationsReportResponse> GetOperationsReportAsync(DateTime? dateFrom, DateTime? dateTo, int? locationId)
        {
            // IMPORTANT: evitar operaciones EF en paralelo sobre el mismo DbContext.
            var (from, toExclusive) = NormalizeDateRange(dateFrom, dateTo);

            var movementQuery = _uow.InventoryMovementRepository.GetAll().AsNoTracking();
            if (locationId.HasValue)
                movementQuery = movementQuery.Where(m => m.LocationId == locationId.Value);
            if (from.HasValue)
                movementQuery = movementQuery.Where(m => m.CreatedAt >= from.Value);
            if (toExclusive.HasValue)
                movementQuery = movementQuery.Where(m => m.CreatedAt < toExclusive.Value);

            var totalMovements = await movementQuery.LongCountAsync();
            var entries = await movementQuery.LongCountAsync(m => m.Type == InventoryMovementType.entry);
            var exits = await movementQuery.LongCountAsync(m => m.Type == InventoryMovementType.exit);
            var adjustments = await movementQuery.LongCountAsync(m => m.Type == InventoryMovementType.adjustment);

            var movementsByTypeRows = await movementQuery
                .GroupBy(m => m.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.LongCount(),
                    QuantitySum = g.Sum(x => x.Quantity)
                })
                .ToListAsync();

            var movementDetailQuery2 = _uow.InventoryMovementRepository
                .GetAllIncluding(m => m.Product, m => m.Supplier)
                .AsNoTracking();

            if (locationId.HasValue)
                movementDetailQuery2 = movementDetailQuery2.Where(m => m.LocationId == locationId.Value);
            if (from.HasValue)
                movementDetailQuery2 = movementDetailQuery2.Where(m => m.CreatedAt >= from.Value);
            if (toExclusive.HasValue)
                movementDetailQuery2 = movementDetailQuery2.Where(m => m.CreatedAt < toExclusive.Value);

            var movementDetailList = await movementDetailQuery2
                .OrderByDescending(m => m.CreatedAt)
                .Take(200)
                .Select(m => new OperationsMovementRowDto
                {
                    Id = m.Id,
                    CreatedAt = m.CreatedAt,
                    Type = m.Type.ToString(),
                    Quantity = m.Quantity,
                    Reason = m.Reason,
                    ReferenceDocument = m.ReferenceDocument,
                    ProductId = m.ProductId,
                    ProductCode = m.Product != null ? m.Product.Code : null,
                    ProductName = m.Product != null ? m.Product.Name : null,
                    SupplierId = m.SupplierId,
                    SupplierName = m.Supplier != null ? m.Supplier.Name : null,
                    LocationId = m.LocationId
                })
                .ToListAsync();

            var entryTypeName = InventoryMovementType.entry.ToString();
            var supplierSummary = movementDetailList
                .Where(m => m.SupplierId.HasValue && m.Type == entryTypeName)
                .GroupBy(m => new { m.SupplierId, m.SupplierName })
                .Select(g => new SupplierSummaryDto
                {
                    SupplierId = g.Key.SupplierId!.Value,
                    SupplierName = g.Key.SupplierName ?? "Sin nombre",
                    TotalEntries = g.Count(),
                    TotalUnits = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalUnits)
                .ToList();

            return new OperationsReportResponse
            {
                TotalMovements = totalMovements,
                Entries = entries,
                Exits = exits,
                Adjustments = adjustments,
                MovementsByType = movementsByTypeRows.Select(x => new MovementsByTypeDto
                {
                    Type = x.Type.ToString(),
                    Count = x.Count,
                    QuantitySum = x.QuantitySum
                }).ToList(),
                MovementDetails = movementDetailList,
                SupplierSummary = supplierSummary
            };
        }
    }
}

