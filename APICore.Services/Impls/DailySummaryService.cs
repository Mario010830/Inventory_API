using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class DailySummaryService : IDailySummaryService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;

        public DailySummaryService(IUnitOfWork uow, CoreDbContext context)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<DailySummaryResponseDto> GenerateAsync(DailySummaryRequestDto request)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new BaseBadRequestException("No se pudo determinar la organización del usuario.");

            // Usuario con location fija → se usa la suya. Admin sin location → debe enviar LocationId.
            var locationId = _context.CurrentLocationId > 0
                ? _context.CurrentLocationId
                : (request.LocationId ?? 0);

            if (locationId <= 0)
                throw new BaseBadRequestException("Debes indicar la localización (LocationId) para generar el cuadre diario.");

            // Validar que la localización pertenece a la organización del usuario
            var locationExists = await _context.Locations
                .IgnoreQueryFilters()
                .AnyAsync(l => l.Id == locationId && l.OrganizationId == orgId);
            if (!locationExists)
                throw new BaseBadRequestException("La localización indicada no pertenece a tu organización.");

            var targetDate = request.Date.Date;

            var existing = await _context.DailySummaries
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.OrganizationId == orgId
                                       && d.LocationId == locationId
                                       && d.Date == targetDate
                                       && d.IsClosed);
            if (existing != null)
                throw new DailySummaryAlreadyClosedBadRequestException();

            // --- Calcular TotalSales ---
            var dayStart = targetDate;
            var dayEnd   = targetDate.AddDays(1);

            var totalSales = await _context.SaleOrders
                .IgnoreQueryFilters()
                .Where(s => s.LocationId == locationId
                         && s.OrganizationId == orgId
                         && s.Status == SaleOrderStatus.confirmed
                         && s.CreatedAt >= dayStart
                         && s.CreatedAt < dayEnd)
                .SumAsync(s => (decimal?)s.Total) ?? 0m;

            // --- Calcular TotalReturns ---
            var totalReturns = await _context.SaleReturns
                .IgnoreQueryFilters()
                .Where(r => r.LocationId == locationId
                         && r.OrganizationId == orgId
                         && r.CreatedAt >= dayStart
                         && r.CreatedAt < dayEnd)
                .SumAsync(r => (decimal?)r.Total) ?? 0m;

            var totalOutflows = await _context.CashOutflows
                .IgnoreQueryFilters()
                .Where(c => c.LocationId == locationId
                         && c.OrganizationId == orgId
                         && c.Date == targetDate)
                .SumAsync(c => (decimal?)c.Amount) ?? 0m;

            // --- Armar InventoryItems por producto vendido ---
            // Se obtienen datos planos primero para evitar problemas de traducción EF con GroupBy + navegación
            var rawLines = await _context.SaleOrderItems
                .IgnoreQueryFilters()
                .Where(i => i.SaleOrder != null
                         && i.SaleOrder.LocationId == locationId
                         && i.SaleOrder.OrganizationId == orgId
                         && i.SaleOrder.Status == SaleOrderStatus.confirmed
                         && i.SaleOrder.CreatedAt >= dayStart
                         && i.SaleOrder.CreatedAt < dayEnd)
                .Select(i => new
                {
                    i.ProductId,
                    ProductName = i.Product != null ? i.Product.Name : string.Empty,
                    i.Quantity
                })
                .ToListAsync();

            var soldItems = rawLines
                .GroupBy(i => new { i.ProductId, i.ProductName })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.ProductName,
                    QuantitySold = g.Sum(i => i.Quantity)
                })
                .ToList();

            var inventoryItems = new List<DailySummaryInventoryItem>();
            foreach (var item in soldItems)
            {
                var inventory = await _context.Inventories
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(inv => inv.ProductId == item.ProductId
                                             && inv.LocationId == locationId);

                var stockAfter  = inventory?.CurrentStock ?? 0m;
                var stockBefore = stockAfter + item.QuantitySold;
                var stockDiff   = stockBefore - stockAfter - item.QuantitySold;

                inventoryItems.Add(new DailySummaryInventoryItem
                {
                    ProductId       = item.ProductId,
                    ProductName     = item.ProductName,
                    QuantitySold    = item.QuantitySold,
                    StockBefore     = stockBefore,
                    StockAfter      = stockAfter,
                    StockDifference = stockDiff
                });
            }

            // --- Calcular campos derivados ---
            var expectedCash = request.OpeningCash + totalSales - totalReturns - totalOutflows;
            var difference   = request.ActualCash - expectedCash;
            var status = difference == 0m
                ? DailySummaryStatus.Balanced
                : (difference > 0m ? DailySummaryStatus.Surplus : DailySummaryStatus.Shortage);

            // Reutilizar o crear el cuadre del día (puede existir uno abierto)
            var dailySummary = await _context.DailySummaries
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.OrganizationId == orgId
                                       && d.LocationId == locationId
                                       && d.Date == targetDate);

            if (dailySummary == null)
            {
                dailySummary = new DailySummary
                {
                    Date           = targetDate,
                    LocationId     = locationId,
                    OrganizationId = orgId,
                };
                await _uow.DailySummaryRepository.AddAsync(dailySummary);
                await _uow.CommitAsync();
            }

            dailySummary.OpeningCash    = request.OpeningCash;
            dailySummary.TotalSales     = totalSales;
            dailySummary.TotalReturns   = totalReturns;
            dailySummary.TotalOutflows  = totalOutflows;
            dailySummary.ExpectedCash   = expectedCash;
            dailySummary.ActualCash     = request.ActualCash;
            dailySummary.Difference     = difference;
            dailySummary.Status         = status;
            dailySummary.Notes          = request.Notes;
            dailySummary.IsClosed       = true;

            // Reemplazar ítems de inventario
            var existingItems = await _context.DailySummaryInventoryItems
                .Where(i => i.DailySummaryId == dailySummary.Id)
                .ToListAsync();
            foreach (var old in existingItems)
                _context.DailySummaryInventoryItems.Remove(old);

            foreach (var inv in inventoryItems)
            {
                inv.DailySummaryId = dailySummary.Id;
                await _uow.DailySummaryInventoryItemRepository.AddAsync(inv);
            }

            _uow.DailySummaryRepository.Update(dailySummary);
            await _uow.CommitAsync();

            return await LoadAndMapAsync(dailySummary.Id);
        }

        public async Task<DailySummaryResponseDto?> GetByDateAsync(DateTime date, int? locationId = null)
        {
            var orgId      = _context.CurrentOrganizationId;
            var resolvedLocationId = ResolveLocationId(locationId);
            var targetDate = date.Date;

            var query = _context.DailySummaries
                .IgnoreQueryFilters()
                .Include(d => d.InventoryItems)
                .Where(d => d.OrganizationId == orgId && d.Date == targetDate);

            if (resolvedLocationId > 0)
                query = query.Where(d => d.LocationId == resolvedLocationId);

            var summary = await query.FirstOrDefaultAsync();
            if (summary == null) return null;

            return await MapToDtoAsync(summary);
        }

        public async Task<List<DailySummaryResponseDto>> GetHistoryAsync(DateTime from, DateTime to, int? locationId = null)
        {
            var orgId              = _context.CurrentOrganizationId;
            var resolvedLocationId = ResolveLocationId(locationId);
            var fromDate           = from.Date;
            var toDate             = to.Date.AddDays(1);

            var query = _context.DailySummaries
                .IgnoreQueryFilters()
                .Include(d => d.InventoryItems)
                .Where(d => d.OrganizationId == orgId
                         && d.Date >= fromDate
                         && d.Date < toDate);

            if (resolvedLocationId > 0)
                query = query.Where(d => d.LocationId == resolvedLocationId);

            var summaries = await query
                .OrderByDescending(d => d.Date)
                .ToListAsync();

            var result = new List<DailySummaryResponseDto>();
            foreach (var s in summaries)
                result.Add(await MapToDtoAsync(s));
            return result;
        }

        public async Task<byte[]> ExportCsvAsync(DateTime date)
        {
            var summary = await GetByDateAsync(date)
                ?? throw new DailySummaryNotFoundException();

            var sb = new StringBuilder();

            sb.AppendLine("CUADRE DIARIO");
            sb.AppendLine($"Fecha;{summary.Date:yyyy-MM-dd}");
            sb.AppendLine($"Estado;{summary.Status}");
            sb.AppendLine($"Cerrado;{(summary.IsClosed ? "Sí" : "No")}");
            if (!string.IsNullOrEmpty(summary.Notes))
                sb.AppendLine($"Notas;{summary.Notes}");
            sb.AppendLine();

            sb.AppendLine("CAJA");
            sb.AppendLine($"Fondo inicial;{summary.OpeningCash:F2}");
            sb.AppendLine($"Total ventas;{summary.TotalSales:F2}");
            sb.AppendLine($"Total devoluciones;{summary.TotalReturns:F2}");
            sb.AppendLine($"Salidas manuales;{summary.TotalOutflows:F2}");
            if (summary.CashOutflows.Count > 0)
            {
                sb.AppendLine("Detalle retiros;Importe;Notas");
                foreach (var o in summary.CashOutflows)
                    sb.AppendLine($"Retiro #{o.Id};{o.Amount:F2};{(o.Notes ?? "").Replace(";", ",")}");
            }
            sb.AppendLine($"Efectivo esperado;{summary.ExpectedCash:F2}");
            sb.AppendLine($"Efectivo contado;{summary.ActualCash:F2}");
            sb.AppendLine($"Diferencia;{summary.Difference:F2}");
            sb.AppendLine();

            sb.AppendLine("INVENTARIO CONSUMIDO");
            sb.AppendLine("Producto;Cant. Vendida;Stock Antes;Stock Después;Diferencia");
            foreach (var item in summary.InventoryItems)
            {
                sb.AppendLine($"{item.ProductName};{item.QuantitySold:F2};{item.StockBefore:F2};{item.StockAfter:F2};{item.StockDifference:F2}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> ExportPdfAsync(DateTime date)
        {
            var summary = await GetByDateAsync(date)
                ?? throw new DailySummaryNotFoundException();

            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(header =>
                    {
                        header.Column(col =>
                        {
                            col.Item().Text("CUADRE DIARIO")
                                .Bold().FontSize(16).AlignCenter();
                            col.Item().Text($"Fecha: {summary.Date:dd/MM/yyyy}")
                                .FontSize(11).AlignCenter();
                            col.Item().Text($"Estado: {summary.Status}  |  Cerrado: {(summary.IsClosed ? "Sí" : "No")}")
                                .AlignCenter();
                            if (!string.IsNullOrEmpty(summary.Notes))
                                col.Item().Text($"Notas: {summary.Notes}").AlignCenter();
                        });
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().PaddingTop(15).Text("RESUMEN DE CAJA").Bold().FontSize(12);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            AddCashRow(table, "Fondo inicial",       summary.OpeningCash);
                            AddCashRow(table, "Total ventas",        summary.TotalSales);
                            AddCashRow(table, "Total devoluciones",  summary.TotalReturns);
                            AddCashRow(table, "Salidas manuales",    summary.TotalOutflows);
                            if (summary.CashOutflows.Count > 0)
                            {
                                foreach (var o in summary.CashOutflows)
                                {
                                    var label = string.IsNullOrEmpty(o.Notes)
                                        ? $"  Retiro #{o.Id}"
                                        : $"  Retiro: {o.Notes}";
                                    AddCashRow(table, label, o.Amount);
                                }
                            }
                            AddCashRow(table, "Efectivo esperado",   summary.ExpectedCash);
                            AddCashRow(table, "Efectivo contado",    summary.ActualCash);
                            AddCashRow(table, "Diferencia",          summary.Difference);
                        });

                        if (summary.InventoryItems.Count > 0)
                        {
                            col.Item().PaddingTop(20).Text("INVENTARIO CONSUMIDO").Bold().FontSize(12);

                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(4);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                AddInventoryHeader(table);
                                foreach (var item in summary.InventoryItems)
                                    AddInventoryRow(table, item);
                            });
                        }
                    });

                    page.Footer()
                        .AlignRight()
                        .Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                            x.Span(" de ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        // ──────────── helpers privados ────────────

        private async Task<DailySummaryResponseDto> LoadAndMapAsync(int id)
        {
            var summary = await _context.DailySummaries
                .Include(d => d.InventoryItems)
                .FirstAsync(d => d.Id == id);
            return await MapToDtoAsync(summary);
        }

        private async Task<DailySummaryResponseDto> MapToDtoAsync(DailySummary d)
        {
            var outflows = await _context.CashOutflows
                .IgnoreQueryFilters()
                .Where(c => c.OrganizationId == d.OrganizationId
                         && c.LocationId == d.LocationId
                         && c.Date == d.Date)
                .OrderBy(c => c.Id)
                .Select(c => new CashOutflowResponseDto
                {
                    Id = c.Id,
                    Date = c.Date,
                    LocationId = c.LocationId,
                    Amount = c.Amount,
                    Notes = c.Notes,
                    UserId = c.UserId,
                    CreatedAt = c.CreatedAt,
                })
                .ToListAsync();

            return new DailySummaryResponseDto
            {
                Id             = d.Id,
                Date           = d.Date,
                LocationId     = d.LocationId,
                OrganizationId = d.OrganizationId,
                OpeningCash    = d.OpeningCash,
                TotalSales     = d.TotalSales,
                TotalReturns   = d.TotalReturns,
                TotalOutflows  = d.TotalOutflows,
                ExpectedCash   = d.ExpectedCash,
                ActualCash     = d.ActualCash,
                Difference     = d.Difference,
                Status         = d.Status,
                Notes          = d.Notes,
                IsClosed       = d.IsClosed,
                CashOutflows   = outflows,
                InventoryItems = d.InventoryItems?
                    .Select(i => new DailySummaryInventoryItemDto
                    {
                        ProductId       = i.ProductId,
                        ProductName     = i.ProductName,
                        QuantitySold    = i.QuantitySold,
                        StockBefore     = i.StockBefore,
                        StockAfter      = i.StockAfter,
                        StockDifference = i.StockDifference
                    }).ToList() ?? new System.Collections.Generic.List<DailySummaryInventoryItemDto>()
            };
        }

        /// <summary>
        /// Si el usuario tiene location fija la usa siempre.
        /// Si es Admin (CurrentLocationId &lt;= 0) usa el parámetro recibido (puede ser null → sin filtro por location).
        /// </summary>
        private int ResolveLocationId(int? requestedLocationId)
            => _context.CurrentLocationId > 0
                ? _context.CurrentLocationId
                : (requestedLocationId ?? 0);

        private static void AddCashRow(TableDescriptor table, string label, decimal value)
        {
            table.Cell().Padding(3).Text(label);
            table.Cell().Padding(3).AlignRight().Text($"{value:F2}");
        }

        private static void AddInventoryHeader(TableDescriptor table)
        {
            foreach (var header in new[] { "Producto", "Cant. Vendida", "Stock Antes", "Stock Después", "Diferencia" })
                table.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(header).Bold();
        }

        private static void AddInventoryRow(TableDescriptor table, DailySummaryInventoryItemDto item)
        {
            table.Cell().Padding(3).Text(item.ProductName);
            table.Cell().Padding(3).AlignRight().Text($"{item.QuantitySold:F2}");
            table.Cell().Padding(3).AlignRight().Text($"{item.StockBefore:F2}");
            table.Cell().Padding(3).AlignRight().Text($"{item.StockAfter:F2}");
            table.Cell().Padding(3).AlignRight().Text($"{item.StockDifference:F2}");
        }
    }
}
