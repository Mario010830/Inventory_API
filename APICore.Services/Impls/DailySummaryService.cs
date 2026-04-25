using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using APICore.Services.Utils;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
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

            // Fecha contable = día civil en Cuba (el front debe enviar la fecha de negocio en Cuba).
            var targetDate = request.Date.Date;
            var (dayStartUtc, dayEndUtc) = CubaBusinessCalendar.GetCubaCalendarDayRangeUtcForCubaDate(targetDate);

            // Periodo del turno: desde el último cierre del mismo día/ubicación, o inicio del día contable.
            var lastClosedAt = await _context.DailySummaries
                .IgnoreQueryFilters()
                .Where(d => d.OrganizationId == orgId
                         && d.LocationId == locationId
                         && d.Date == targetDate
                         && d.IsClosed
                         && d.ClosedAt != null)
                .OrderByDescending(d => d.ClosedAt)
                .Select(d => d.ClosedAt)
                .FirstOrDefaultAsync();

            var periodStart = lastClosedAt ?? dayStartUtc;
            if (periodStart < dayStartUtc)
                periodStart = dayStartUtc;
            if (periodStart > dayEndUtc)
                throw new BaseBadRequestException("El periodo del cuadre no es válido para la fecha indicada.");

            var periodEnd = DateTime.UtcNow;
            if (periodEnd < periodStart)
                throw new BaseBadRequestException("No se puede cerrar un cuadre con un periodo vacío.");

            // --- Calcular TotalSales (hora de confirmación ≈ ModifiedAt) ---
            var totalSales = await _context.SaleOrders
                .IgnoreQueryFilters()
                .Where(s => s.LocationId == locationId
                         && s.OrganizationId == orgId
                         && s.Status == SaleOrderStatus.confirmed
                         && s.ModifiedAt >= periodStart
                         && s.ModifiedAt < periodEnd)
                .SumAsync(s => (decimal?)s.Total) ?? 0m;

            // --- Calcular TotalReturns (solo completadas; momento operativo ≈ ModifiedAt) ---
            var totalReturns = await _context.SaleReturns
                .IgnoreQueryFilters()
                .Where(r => r.LocationId == locationId
                         && r.OrganizationId == orgId
                         && r.Status == SaleReturnStatus.completed
                         && r.ModifiedAt >= periodStart
                         && r.ModifiedAt < periodEnd)
                .SumAsync(r => (decimal?)r.Total) ?? 0m;

            var totalOutflows = await _context.CashOutflows
                .IgnoreQueryFilters()
                .Where(c => c.LocationId == locationId
                         && c.OrganizationId == orgId
                         && c.CreatedAt >= periodStart
                         && c.CreatedAt < periodEnd)
                .SumAsync(c => (decimal?)c.Amount) ?? 0m;

            // --- Armar InventoryItems por producto vendido ---
            // Se obtienen datos planos primero para evitar problemas de traducción EF con GroupBy + navegación
            var rawLines = await _context.SaleOrderItems
                .IgnoreQueryFilters()
                .Where(i => i.SaleOrder != null
                         && i.SaleOrder.LocationId == locationId
                         && i.SaleOrder.OrganizationId == orgId
                         && i.SaleOrder.Status == SaleOrderStatus.confirmed
                         && i.SaleOrder.ModifiedAt >= periodStart
                         && i.SaleOrder.ModifiedAt < periodEnd)
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

            // Un registro por cierre de turno (varios el mismo día contable).
            var dailySummary = new DailySummary
            {
                Date = targetDate,
                PeriodStart = periodStart,
                ClosedAt = periodEnd,
                LocationId = locationId,
                OrganizationId = orgId,
                OpeningCash = request.OpeningCash,
                TotalSales = totalSales,
                TotalReturns = totalReturns,
                TotalOutflows = totalOutflows,
                ExpectedCash = expectedCash,
                ActualCash = request.ActualCash,
                Difference = difference,
                Status = status,
                Notes = request.Notes,
                IsClosed = true,
            };
            foreach (var inv in inventoryItems)
                dailySummary.InventoryItems.Add(inv);

            await _uow.DailySummaryRepository.AddAsync(dailySummary);
            await _uow.CommitAsync();

            return await LoadAndMapAsync(dailySummary.Id);
        }

        public async Task<IReadOnlyList<DailySummaryResponseDto>> GetByDateAsync(DateTime date, int? locationId = null)
        {
            var orgId = _context.CurrentOrganizationId;
            var resolvedLocationId = ResolveLocationId(locationId);
            if (resolvedLocationId <= 0)
                throw new BaseBadRequestException("Debes indicar la localización (LocationId) para consultar el cuadre diario.");
            var targetDate = date.Date;

            var query = _context.DailySummaries
                .IgnoreQueryFilters()
                .Include(d => d.InventoryItems)
                .Where(d => d.OrganizationId == orgId && d.Date == targetDate);

            query = query.Where(d => d.LocationId == resolvedLocationId);

            var summaries = await query
                .OrderBy(d => d.PeriodStart)
                .ThenBy(d => d.Id)
                .ToListAsync();

            var result = new List<DailySummaryResponseDto>();
            foreach (var s in summaries)
                result.Add(await MapToDtoAsync(s));
            return result;
        }

        public async Task<DailySummaryResponseDto?> GetByIdAsync(int id)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                return null;

            var query = _context.DailySummaries
                .IgnoreQueryFilters()
                .Include(d => d.InventoryItems)
                .Where(d => d.Id == id && d.OrganizationId == orgId);

            if (_context.CurrentLocationId > 0)
                query = query.Where(d => d.LocationId == _context.CurrentLocationId);

            var summary = await query.FirstOrDefaultAsync();
            return summary == null ? null : await MapToDtoAsync(summary);
        }

        public async Task<List<DailySummaryResponseDto>> GetHistoryAsync(DateTime from, DateTime to, int? locationId = null)
        {
            var orgId              = _context.CurrentOrganizationId;
            var resolvedLocationId = ResolveLocationId(locationId);
            if (resolvedLocationId <= 0)
                throw new BaseBadRequestException("Debes indicar la localización (LocationId) para el historial de cuadres.");
            var fromDate           = from.Date;
            var toDate             = to.Date.AddDays(1);

            var query = _context.DailySummaries
                .IgnoreQueryFilters()
                .Include(d => d.InventoryItems)
                .Where(d => d.OrganizationId == orgId
                         && d.Date >= fromDate
                         && d.Date < toDate);

            query = query.Where(d => d.LocationId == resolvedLocationId);

            var summaries = await query
                .OrderByDescending(d => d.Date)
                .ThenByDescending(d => d.ClosedAt)
                .ThenByDescending(d => d.Id)
                .ToListAsync();

            var result = new List<DailySummaryResponseDto>();
            foreach (var s in summaries)
                result.Add(await MapToDtoAsync(s));
            return result;
        }

        public async Task<byte[]> ExportCsvAsync(DailySummaryExportRequestDto request)
        {
            var summary = await ResolveExportSummaryAsync(request)
                ?? throw new DailySummaryNotFoundException();

            var sb = new StringBuilder();

            sb.AppendLine("CUADRE DIARIO");
            sb.AppendLine($"Fecha;{summary.Date:yyyy-MM-dd}");
            sb.AppendLine($"Periodo (hora Cuba);{FormatCubaPeriodLine(summary)}");
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

        public async Task<byte[]> ExportPdfAsync(DailySummaryExportRequestDto request)
        {
            var summary = await ResolveExportSummaryAsync(request)
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
                            col.Item().Text($"Periodo (hora Cuba): {FormatCubaPeriodLine(summary)}")
                                .FontSize(10).AlignCenter();
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

        private async Task<DailySummaryResponseDto?> ResolveExportSummaryAsync(DailySummaryExportRequestDto request)
        {
            if (request.Id.HasValue)
                return await GetByIdAsync(request.Id.Value);

            var list = await GetByDateAsync(request.Date, request.LocationId);
            if (list.Count == 0)
                return null;
            return list.OrderByDescending(s => s.ClosedAt ?? DateTime.MinValue).First();
        }

        private async Task<DailySummaryResponseDto> LoadAndMapAsync(int id)
        {
            var summary = await _context.DailySummaries
                .Include(d => d.InventoryItems)
                .FirstAsync(d => d.Id == id);
            return await MapToDtoAsync(summary);
        }

        private async Task<DailySummaryResponseDto> MapToDtoAsync(DailySummary d)
        {
            var periodEndExclusive = d.ClosedAt ?? d.ModifiedAt;
            var outflows = await _context.CashOutflows
                .IgnoreQueryFilters()
                .Where(c => c.OrganizationId == d.OrganizationId
                         && c.LocationId == d.LocationId
                         && c.CreatedAt >= d.PeriodStart
                         && c.CreatedAt < periodEndExclusive)
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

            var (physSurplus, physShortage, physNet) =
                await PhysicalInventoryCountService.GetValuedTotalsForDailySummaryAsync(_context, d.Id);

            return new DailySummaryResponseDto
            {
                Id             = d.Id,
                Date           = d.Date,
                PeriodStart    = d.PeriodStart,
                ClosedAt       = d.ClosedAt,
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
                PhysicalCountTotalSurplusValued = physSurplus,
                PhysicalCountTotalShortageValued = physShortage,
                PhysicalCountNetValuedImpact = physNet,
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

        /// <summary>Convierte un instante almacenado en UTC (timestamptz) a texto en hora civil de Cuba.</summary>
        private static string FormatCubaLocalFromUtc(DateTime utcInstant)
        {
            var utc = utcInstant.Kind switch
            {
                DateTimeKind.Utc => utcInstant,
                DateTimeKind.Local => utcInstant.ToUniversalTime(),
                _ => DateTime.SpecifyKind(utcInstant, DateTimeKind.Utc),
            };
            return TimeZoneInfo.ConvertTimeFromUtc(utc, CubaBusinessCalendar.CubaTimeZone)
                .ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("es-ES"));
        }

        private static string FormatCubaPeriodLine(DailySummaryResponseDto summary)
        {
            var start = FormatCubaLocalFromUtc(summary.PeriodStart);
            if (!summary.ClosedAt.HasValue)
                return $"{start} → —";
            return $"{start} → {FormatCubaLocalFromUtc(summary.ClosedAt.Value)}";
        }
    }
}
