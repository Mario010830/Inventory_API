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
            var locationId = _context.CurrentLocationId;

            var targetDate = request.Date.Date;

            var existing = await _context.DailySummaries
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
                .Where(s => s.LocationId == locationId
                         && s.OrganizationId == orgId
                         && s.Status == SaleOrderStatus.confirmed
                         && s.CreatedAt >= dayStart
                         && s.CreatedAt < dayEnd)
                .SumAsync(s => (decimal?)s.Total) ?? 0m;

            // --- Calcular TotalReturns ---
            var totalReturns = await _context.SaleReturns
                .Where(r => r.LocationId == locationId
                         && r.OrganizationId == orgId
                         && r.CreatedAt >= dayStart
                         && r.CreatedAt < dayEnd)
                .SumAsync(r => (decimal?)r.Total) ?? 0m;

            // --- Armar InventoryItems por producto vendido ---
            var soldItems = await _context.SaleOrderItems
                .Include(i => i.SaleOrder)
                .Include(i => i.Product)
                .Where(i => i.SaleOrder != null
                         && i.SaleOrder.LocationId == locationId
                         && i.SaleOrder.OrganizationId == orgId
                         && i.SaleOrder.Status == SaleOrderStatus.confirmed
                         && i.SaleOrder.CreatedAt >= dayStart
                         && i.SaleOrder.CreatedAt < dayEnd)
                .GroupBy(i => new { i.ProductId, ProductName = i.Product != null ? i.Product.Name : string.Empty })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.ProductName,
                    QuantitySold = g.Sum(i => i.Quantity)
                })
                .ToListAsync();

            var inventoryItems = new List<DailySummaryInventoryItem>();
            foreach (var item in soldItems)
            {
                var inventory = await _context.Inventories
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
            var expectedCash = request.OpeningCash + totalSales - totalReturns;
            var difference   = request.ActualCash - expectedCash;
            var status = difference == 0m
                ? DailySummaryStatus.Balanced
                : (difference > 0m ? DailySummaryStatus.Surplus : DailySummaryStatus.Shortage);

            // Reutilizar o crear el cuadre del día (puede existir uno abierto)
            var dailySummary = await _context.DailySummaries
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
            dailySummary.TotalOutflows  = 0m;
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

        public async Task<DailySummaryResponseDto?> GetByDateAsync(DateTime date)
        {
            var orgId      = _context.CurrentOrganizationId;
            var locationId = _context.CurrentLocationId;
            var targetDate = date.Date;

            var summary = await _context.DailySummaries
                .Include(d => d.InventoryItems)
                .FirstOrDefaultAsync(d => d.OrganizationId == orgId
                                       && d.LocationId == locationId
                                       && d.Date == targetDate);
            if (summary == null) return null;

            return MapToDto(summary);
        }

        public async Task<List<DailySummaryResponseDto>> GetHistoryAsync(DateTime from, DateTime to)
        {
            var orgId      = _context.CurrentOrganizationId;
            var locationId = _context.CurrentLocationId;
            var fromDate   = from.Date;
            var toDate     = to.Date.AddDays(1);

            var summaries = await _context.DailySummaries
                .Include(d => d.InventoryItems)
                .Where(d => d.OrganizationId == orgId
                         && d.LocationId == locationId
                         && d.Date >= fromDate
                         && d.Date < toDate)
                .OrderByDescending(d => d.Date)
                .ToListAsync();

            return summaries.Select(MapToDto).ToList();
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
            return MapToDto(summary);
        }

        private static DailySummaryResponseDto MapToDto(DailySummary d) =>
            new DailySummaryResponseDto
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
