using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class PhysicalInventoryCountService : IPhysicalInventoryCountService
    {
        private const decimal QtyTolerance = 0.0001m;

        private readonly CoreDbContext _context;
        private readonly IUnitOfWork _uow;
        private readonly IInventorySettings _inventorySettings;

        public PhysicalInventoryCountService(CoreDbContext context, IUnitOfWork uow, IInventorySettings inventorySettings)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _inventorySettings = inventorySettings ?? throw new ArgumentNullException(nameof(inventorySettings));
        }

        public async Task<PhysicalInventoryCountDetailResponse> GenerateExpectedAsync(int dailySummaryId, int userId)
        {
            var summary = await LoadDailySummaryAsync(dailySummaryId);
            if (!summary.ClosedAt.HasValue)
                throw new BaseBadRequestException("El cuadre debe estar cerrado para generar el conteo físico.");

            var periodEnd = summary.ClosedAt.Value;

            var existing = await _context.PhysicalInventoryCounts
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.DailySummaryId == dailySummaryId);

            if (existing != null)
            {
                if (string.Equals(existing.Status, PhysicalInventoryCountStatus.Finalized, StringComparison.OrdinalIgnoreCase))
                    throw new BaseBadRequestException("Ya existe un conteo físico finalizado para este cuadre.");
                _context.PhysicalInventoryCounts.Remove(existing);
                await _context.SaveChangesAsync();
            }

            var soldByProduct = await GetSoldQuantitiesByProductAsync(summary, periodEnd);
            var returnedByProduct = await GetReturnedQuantitiesByProductAsync(summary, periodEnd);
            var productIds = soldByProduct.Keys
                .Union(returnedByProduct.Keys)
                .Distinct()
                .ToList();

            if (productIds.Count == 0)
                throw new BaseBadRequestException("No hay productos con ventas o devoluciones en el periodo de este cuadre.");

            var products = await _context.Products
                .IgnoreQueryFilters()
                .Where(p => productIds.Contains(p.Id) && p.OrganizationId == summary.OrganizationId)
                .ToDictionaryAsync(p => p.Id);

            var qtyDecimals = _inventorySettings.RoundingDecimals;
            var priceDecimals = _inventorySettings.PriceRoundingDecimals;

            var entity = new PhysicalInventoryCount
            {
                DailySummaryId = dailySummaryId,
                CountedAt = DateTime.UtcNow,
                UserId = userId > 0 ? userId : null,
                Status = PhysicalInventoryCountStatus.Draft,
            };

            foreach (var pid in productIds.Where(products.ContainsKey).OrderBy(id => products[id].Name))
            {
                var product = products[pid];

                var currentStock = await GetCurrentStockAsync(pid, summary.LocationId);
                var sold = soldByProduct.GetValueOrDefault(pid);
                var returned = returnedByProduct.GetValueOrDefault(pid);
                var expected = Math.Round(currentStock + sold - returned, qtyDecimals, MidpointRounding.AwayFromZero);
                var unitPrice = Math.Round(product.Precio, priceDecimals, MidpointRounding.AwayFromZero);

                entity.Items.Add(new PhysicalInventoryCountItem
                {
                    ProductId = pid,
                    ProductName = product.Name ?? string.Empty,
                    ExpectedQuantity = expected,
                    CountedQuantity = null,
                    Difference = 0,
                    UnitPrice = unitPrice,
                    ValuedDifference = 0,
                });
            }

            if (entity.Items.Count == 0)
                throw new BaseBadRequestException("No se pudieron generar líneas de conteo para los productos del periodo.");

            await _uow.PhysicalInventoryCountRepository.AddAsync(entity);
            await _uow.CommitAsync();

            return MapDetail(await _context.PhysicalInventoryCounts
                .Include(p => p.Items)
                .FirstAsync(p => p.Id == entity.Id));
        }

        public async Task<PhysicalInventoryCountDetailResponse> SaveItemsAsync(int physicalInventoryCountId, SavePhysicalInventoryCountItemsRequest request)
        {
            var count = await _context.PhysicalInventoryCounts
                .Include(p => p.Items)
                .Include(p => p.DailySummary)
                .FirstOrDefaultAsync(p => p.Id == physicalInventoryCountId);

            if (count?.DailySummary == null)
                throw new BaseNotFoundException("Conteo físico no encontrado.");

            EnsureOrgAccess(count.DailySummary);

            if (!string.Equals(count.Status, PhysicalInventoryCountStatus.Draft, StringComparison.OrdinalIgnoreCase))
                throw new BaseBadRequestException("Solo se pueden guardar ítems de un conteo en estado borrador.");

            if (request.Items.Count != count.Items.Count)
                throw new BaseBadRequestException("Debe enviar una línea por cada producto del conteo.");

            var byProduct = request.Items.ToDictionary(i => i.ProductId, i => i.CountedQuantity);
            var qtyDecimals = _inventorySettings.RoundingDecimals;
            var priceDecimals = _inventorySettings.PriceRoundingDecimals;

            foreach (var line in count.Items)
            {
                if (!byProduct.TryGetValue(line.ProductId, out var countedRaw))
                    throw new BaseBadRequestException($"Falta la cantidad contada para el producto {line.ProductId}.");

                var counted = Math.Round(countedRaw, qtyDecimals, MidpointRounding.AwayFromZero);
                var diff = Math.Round(counted - line.ExpectedQuantity, qtyDecimals, MidpointRounding.AwayFromZero);
                var valued = Math.Round(diff * line.UnitPrice, priceDecimals, MidpointRounding.AwayFromZero);

                line.CountedQuantity = counted;
                line.Difference = diff;
                line.ValuedDifference = valued;
            }

            count.Status = PhysicalInventoryCountStatus.Finalized;
            count.CountedAt = DateTime.UtcNow;
            _uow.PhysicalInventoryCountRepository.Update(count);
            await _uow.CommitAsync();

            return MapDetail(await _context.PhysicalInventoryCounts
                .Include(p => p.Items)
                .FirstAsync(p => p.Id == count.Id));
        }

        public async Task<PhysicalInventoryCountSummaryResponse> GetSummaryAsync(int dailySummaryId)
        {
            await EnsureDailySummaryExistsAsync(dailySummaryId);

            var count = await _context.PhysicalInventoryCounts
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.DailySummaryId == dailySummaryId);

            if (count == null)
            {
                return new PhysicalInventoryCountSummaryResponse
                {
                    PhysicalInventoryCountId = null,
                    Status = "none",
                    Lines = new List<PhysicalInventoryCountSummaryLineResponse>(),
                };
            }

            return BuildSummary(count);
        }

        /// <summary>Totales valorizados del conteo finalizado (para enriquecer el DTO del cuadre).</summary>
        public static async Task<(decimal? surplus, decimal? shortage, decimal? net)> GetValuedTotalsForDailySummaryAsync(
            CoreDbContext context, int dailySummaryId)
        {
            var count = await context.PhysicalInventoryCounts
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.DailySummaryId == dailySummaryId
                    && p.Status == PhysicalInventoryCountStatus.Finalized);

            if (count == null)
                return (null, null, null);

            var totalSurplus = count.Items.Where(i => i.ValuedDifference > 0).Sum(i => i.ValuedDifference);
            var totalShortage = count.Items.Where(i => i.ValuedDifference < 0).Sum(i => -i.ValuedDifference);
            var net = count.Items.Sum(i => i.ValuedDifference);
            return (totalSurplus, totalShortage, net);
        }

        private static PhysicalInventoryCountSummaryResponse BuildSummaryStatic(PhysicalInventoryCount count)
        {
            var lines = count.Items
                .OrderBy(i => i.ProductName)
                .Select(i => new PhysicalInventoryCountSummaryLineResponse
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ExpectedQuantity = i.ExpectedQuantity,
                    CountedQuantity = i.CountedQuantity,
                    Difference = i.Difference,
                    ValuedDifference = i.ValuedDifference,
                    Classification = Classify(i.Difference),
                })
                .ToList();

            var totalSurplus = count.Items.Where(i => i.ValuedDifference > 0).Sum(i => i.ValuedDifference);
            var totalShortage = count.Items.Where(i => i.ValuedDifference < 0).Sum(i => -i.ValuedDifference);
            var net = count.Items.Sum(i => i.ValuedDifference);

            return new PhysicalInventoryCountSummaryResponse
            {
                PhysicalInventoryCountId = count.Id,
                Status = count.Status,
                Lines = lines,
                TotalSurplusValued = totalSurplus,
                TotalShortageValued = totalShortage,
                NetValuedImpact = net,
            };
        }

        private PhysicalInventoryCountSummaryResponse BuildSummary(PhysicalInventoryCount count) =>
            BuildSummaryStatic(count);

        private async Task<DailySummary> LoadDailySummaryAsync(int dailySummaryId)
        {
            var summary = await _context.DailySummaries
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Id == dailySummaryId);

            if (summary == null)
                throw new BaseNotFoundException("Cuadre diario no encontrado.");

            EnsureOrgAccess(summary);
            return summary;
        }

        private async Task EnsureDailySummaryExistsAsync(int dailySummaryId)
        {
            _ = await LoadDailySummaryAsync(dailySummaryId);
        }

        private void EnsureOrgAccess(DailySummary summary)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0 || summary.OrganizationId != orgId)
                throw new BaseNotFoundException("Cuadre diario no encontrado.");

            if (_context.CurrentLocationId > 0 && summary.LocationId != _context.CurrentLocationId)
                throw new BaseNotFoundException("Cuadre diario no encontrado.");
        }

        private async Task<Dictionary<int, decimal>> GetSoldQuantitiesByProductAsync(DailySummary summary, DateTime periodEndExclusive)
        {
            var raw = await _context.SaleOrderItems
                .IgnoreQueryFilters()
                .Where(i => i.SaleOrder != null
                    && i.SaleOrder.LocationId == summary.LocationId
                    && i.SaleOrder.OrganizationId == summary.OrganizationId
                    && i.SaleOrder.Status == SaleOrderStatus.confirmed
                    && i.SaleOrder.ModifiedAt >= summary.PeriodStart
                    && i.SaleOrder.ModifiedAt < periodEndExclusive)
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToListAsync();

            return raw.ToDictionary(x => x.ProductId, x => x.Qty);
        }

        private async Task<Dictionary<int, decimal>> GetReturnedQuantitiesByProductAsync(DailySummary summary, DateTime periodEndExclusive)
        {
            var raw = await _context.SaleReturnItems
                .IgnoreQueryFilters()
                .Where(i => i.SaleReturn != null
                    && i.SaleReturn.LocationId == summary.LocationId
                    && i.SaleReturn.OrganizationId == summary.OrganizationId
                    && i.SaleReturn.Status == SaleReturnStatus.completed
                    && i.SaleReturn.ModifiedAt >= summary.PeriodStart
                    && i.SaleReturn.ModifiedAt < periodEndExclusive)
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToListAsync();

            return raw.ToDictionary(x => x.ProductId, x => x.Qty);
        }

        private async Task<decimal> GetCurrentStockAsync(int productId, int locationId)
        {
            var inv = await _context.Inventories
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.LocationId == locationId);
            return inv?.CurrentStock ?? 0m;
        }

        private static string Classify(decimal difference)
        {
            if (Math.Abs(difference) < QtyTolerance)
                return "OK";
            return difference > 0 ? "Sobrante" : "Faltante";
        }

        private static PhysicalInventoryCountDetailResponse MapDetail(PhysicalInventoryCount p) =>
            new()
            {
                Id = p.Id,
                DailySummaryId = p.DailySummaryId,
                CountedAt = p.CountedAt,
                UserId = p.UserId,
                Status = p.Status,
                Items = p.Items
                    .OrderBy(i => i.ProductName)
                    .Select(i => new PhysicalInventoryCountItemResponse
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        ExpectedQuantity = i.ExpectedQuantity,
                        CountedQuantity = i.CountedQuantity,
                        Difference = i.Difference,
                        UnitPrice = i.UnitPrice,
                        ValuedDifference = i.ValuedDifference,
                    })
                    .ToList(),
            };
    }
}
