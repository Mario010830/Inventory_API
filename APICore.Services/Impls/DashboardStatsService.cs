using APICore.Common.DTO.Response;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Data.UoW;
using APICore.Services.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class DashboardStatsService : IDashboardStatsService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IInventorySettings _inventorySettings;

        public DashboardStatsService(IUnitOfWork uow, CoreDbContext context, IInventorySettings inventorySettings)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _inventorySettings = inventorySettings ?? throw new ArgumentNullException(nameof(inventorySettings));
        }

        public async Task<DashboardSummaryResponse> GetDashboardSummaryAsync(DateTime? from, DateTime? to)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                return new DashboardSummaryResponse();

            var products = _uow.ProductRepository.GetAll().Where(p => !p.IsDeleted);
            var totalProducts = await products.CountAsync();

            var minStock = _inventorySettings.DefaultMinimumStock;
            var inventories = _uow.InventoryRepository.GetAllIncluding(i => i.Product)
                .Where(i => i.Product != null && !i.Product.IsDeleted);
            var inventoryValue = await inventories.SumAsync(i => i.CurrentStock * (i.Product != null ? i.Product.Costo : 0));
            var lowStockCount = await inventories.CountAsync(i => i.CurrentStock <= minStock);

            var now = DateTime.UtcNow;
            var (weekStartUtc, weekEndUtcExclusive) = CubaBusinessCalendar.GetCubaWeekRangeUtc(now);
            var movements = _uow.InventoryMovementRepository.GetAll();
            var weeklyOrders = await movements.CountAsync(m => m.Type == InventoryMovementType.entry && m.CreatedAt >= weekStartUtc && m.CreatedAt < weekEndUtcExclusive);

            return new DashboardSummaryResponse
            {
                TotalProducts = totalProducts,
                TotalProductsTrend = 12,
                InventoryValue = inventoryValue,
                InventoryValueTrend = 5.4m,
                LowStockCount = lowStockCount,
                LowStockChange = -2,
                WeeklyOrders = weeklyOrders,
                WeeklyOrdersTrend = 22
            };
        }

        public async Task<ChartLineResponse> GetInventoryFlowAsync(int days, DateTime? from, DateTime? to)
        {
            var (startUtc, endUtcExclusive, loopStartCuba, loopEndCuba) = CubaBusinessCalendar.ResolveMovementFlowRange(days, from, to);

            var movements = await _uow.InventoryMovementRepository.GetAll()
                .Where(m => m.CreatedAt >= startUtc && m.CreatedAt < endUtcExclusive)
                .ToListAsync();

            var byDay = movements
                .GroupBy(m => TimeZoneInfo.ConvertTimeFromUtc(m.CreatedAt, CubaBusinessCalendar.CubaTimeZone).Date)
                .ToDictionary(g => g.Key, g => g.Count());
            var culture = new CultureInfo("es-ES");
            var data = new List<ChartDataPointResponse>();
            for (var d = loopStartCuba; d <= loopEndCuba; d = d.AddDays(1))
            {
                var count = byDay.TryGetValue(d, out var c) ? c : 0;
                data.Add(new ChartDataPointResponse
                {
                    Label = culture.DateTimeFormat.GetAbbreviatedDayName(d.DayOfWeek),
                    Date = d.ToString("yyyy-MM-dd"),
                    Value = count
                });
            }
            return new ChartLineResponse { Data = data };
        }

        public async Task<ChartDonutResponse> GetCategoryDistributionAsync()
        {
            var products = _uow.ProductRepository.GetAllIncluding(p => p.Category).Where(p => !p.IsDeleted);
            var byCategory = await products
                .Where(p => p.Category != null)
                .GroupBy(p => p.Category!.Name)
                .Select(g => new ChartDonutItemResponse { Name = g.Key ?? "Sin categoría", Value = g.Count() })
                .ToListAsync();
            return new ChartDonutResponse { Data = byCategory };
        }

        public async Task<ChartLineResponse> GetInventoryValueEvolutionAsync(int months, DateTime? from, DateTime? to)
        {
            var end = (to ?? DateTime.UtcNow).Date;
            var start = (from ?? end.AddMonths(-months)).Date;
            if (start > end) start = end.AddMonths(-months);
            var totalValue = await _uow.InventoryRepository.GetAllIncluding(i => i.Product)
                .Where(i => i.Product != null && !i.Product.IsDeleted)
                .SumAsync(i => i.CurrentStock * (i.Product != null ? i.Product.Costo : 0));
            var culture = new CultureInfo("es-ES");
            var data = new List<ChartDataPointResponse>();
            for (var d = start; d <= end; d = d.AddMonths(1))
            {
                data.Add(new ChartDataPointResponse
                {
                    Label = culture.DateTimeFormat.GetAbbreviatedMonthName(d.Month),
                    Date = d.ToString("yyyy-MM-dd"),
                    Value = totalValue
                });
            }
            return new ChartLineResponse { Data = data };
        }

        public async Task<ChartDonutResponse> GetStockStatusAsync()
        {
            var minStock = _inventorySettings.DefaultMinimumStock;
            var inventories = _uow.InventoryRepository.GetAll();
            var total = await inventories.CountAsync();
            if (total == 0)
                return new ChartDonutResponse();
            var inRange = await inventories.CountAsync(i => i.CurrentStock > minStock);
            var low = await inventories.CountAsync(i => i.CurrentStock <= minStock && i.CurrentStock > 0);
            var critical = await inventories.CountAsync(i => i.CurrentStock <= 0);
            var data = new List<ChartDonutItemResponse>
            {
                new ChartDonutItemResponse { Name = "En rango", Value = total > 0 ? Math.Round(100m * inRange / total, 0) : 0 },
                new ChartDonutItemResponse { Name = "Bajo", Value = total > 0 ? Math.Round(100m * low / total, 0) : 0 },
                new ChartDonutItemResponse { Name = "Crítico", Value = total > 0 ? Math.Round(100m * critical / total, 0) : 0 }
            };
            return new ChartDonutResponse { Data = data };
        }

        public async Task<ListCardResponse> GetListTopMovementsAsync(int days, int limit)
        {
            var (cubaTodayStartUtc, _) = CubaBusinessCalendar.GetCubaCalendarDayRangeUtc(DateTime.UtcNow);
            var start = cubaTodayStartUtc.AddDays(-days);
            var byProduct = await _uow.InventoryMovementRepository.GetAll()
                .Where(m => m.CreatedAt >= start)
                .GroupBy(m => m.ProductId)
                .Select(g => new { ProductId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(limit)
                .ToListAsync();
            var productIds = byProduct.Select(x => x.ProductId).ToList();
            var products = await _uow.ProductRepository.GetAll()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);
            var data = byProduct.Select(x => new ListCardItemResponse
            {
                Primary = products.TryGetValue(x.ProductId, out var name) ? name : "—",
                Secondary = $"{x.Count} mov. · Últimos {days} días"
            }).ToList();
            return new ListCardResponse { Data = data };
        }

        public async Task<ListCardResponse> GetListLowStockAsync(int limit)
        {
            var minStock = _inventorySettings.DefaultMinimumStock;
            var unit = _inventorySettings.DefaultUnitOfMeasure;

            var lowStockByProduct = await _uow.InventoryRepository.GetAllIncluding(i => i.Product)
                .Where(i => i.Product != null && !i.Product.IsDeleted)
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, TotalStock = g.Sum(i => i.CurrentStock) })
                .Where(x => x.TotalStock <= minStock)
                .OrderBy(x => x.TotalStock)
                .Take(limit)
                .ToListAsync();

            if (lowStockByProduct.Count == 0)
                return new ListCardResponse { Data = new List<ListCardItemResponse>() };

            var productIds = lowStockByProduct.Select(x => x.ProductId).ToList();
            var productNames = await _uow.ProductRepository.GetAll()
                .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                .ToDictionaryAsync(p => p.Id, p => p.Name ?? "—");

            var data = lowStockByProduct.Select(x => new ListCardItemResponse
            {
                Primary = productNames.TryGetValue(x.ProductId, out var name) ? name : "—",
                Secondary = $"{x.TotalStock} {unit} · Mín. {minStock}"
            }).ToList();

            return new ListCardResponse { Data = data };
        }

        public async Task<ListCardResponse> GetListLatestMovementsAsync(int limit)
        {
            var movements = await _uow.InventoryMovementRepository.GetAllIncluding(m => m.Product)
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .ToListAsync();
            var data = movements.Select(m =>
            {
                var typeLabel = m.Type == InventoryMovementType.entry ? "Entrada" : m.Type == InventoryMovementType.exit ? "Salida" : "Ajuste";
                var productName = m.Product?.Name ?? "—";
                var ago = FormatTimeAgo(DateTime.UtcNow - m.CreatedAt);
                return new ListCardItemResponse
                {
                    Primary = $"{typeLabel} · {productName}",
                    Secondary = $"Hace {ago}"
                };
            }).ToList();
            return new ListCardResponse { Data = data };
        }

        public async Task<ListCardResponse> GetListValueByLocationAsync(int limit)
        {
            var byLocation = await _uow.InventoryRepository.GetAllIncluding(i => i.Product, i => i.Location)
                .Where(i => i.Location != null && i.Product != null && !i.Product.IsDeleted)
                .GroupBy(i => i.LocationId)
                .Select(g => new
                {
                    LocationId = g.Key,
                    Value = g.Sum(x => x.CurrentStock * (x.Product != null ? x.Product.Costo : 0))
                })
                .OrderByDescending(x => x.Value)
                .Take(limit)
                .ToListAsync();
            var locIds = byLocation.Select(x => x.LocationId).ToList();
            var locations = await _uow.LocationRepository.GetAll()
                .Where(l => locIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, l => l.Name);
            var data = byLocation.Select(x =>
            {
                var name = locations.TryGetValue(x.LocationId, out var n) ? n : "—";
                var secondary = x.Value >= 1000 ? $"${x.Value / 1000:F1}k" : $"${x.Value:F0}";
                return new ListCardItemResponse { Primary = name, Secondary = secondary };
            }).ToList();
            return new ListCardResponse { Data = data };
        }

        public async Task<ListCardResponse> GetListRecentProductsAsync(int limit, int days)
        {
            var start = DateTime.UtcNow.AddDays(-days);
            var products = await _uow.ProductRepository.GetAll()
                .Where(p => !p.IsDeleted && p.CreatedAt >= start)
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync();
            var data = products.Select(p => new ListCardItemResponse
            {
                Primary = p.Name,
                Secondary = $"Añadido hace {FormatTimeAgo(DateTime.UtcNow - p.CreatedAt)}"
            }).ToList();
            return new ListCardResponse { Data = data };
        }

        public async Task<ChartComposedResponse> GetEntriesVsExitsAsync(int days, DateTime? from, DateTime? to)
        {
            var (startUtc, endUtcExclusive, loopStartCuba, loopEndCuba) = CubaBusinessCalendar.ResolveMovementFlowRange(days, from, to);
            var movements = await _uow.InventoryMovementRepository.GetAll()
                .Where(m => m.CreatedAt >= startUtc && m.CreatedAt < endUtcExclusive)
                .ToListAsync();
            var byDay = movements
                .GroupBy(m => TimeZoneInfo.ConvertTimeFromUtc(m.CreatedAt, CubaBusinessCalendar.CubaTimeZone).Date)
                .ToDictionary(g => g.Key, g => g.ToList());
            var culture = new CultureInfo("es-ES");
            var data = new List<ChartComposedPointResponse>();
            for (var d = loopStartCuba; d <= loopEndCuba; d = d.AddDays(1))
            {
                var list = byDay.TryGetValue(d, out var l) ? l : new List<InventoryMovement>();
                var entries = list.Count(x => x.Type == InventoryMovementType.entry);
                var exits = list.Count(x => x.Type == InventoryMovementType.exit);
                data.Add(new ChartComposedPointResponse
                {
                    Label = culture.DateTimeFormat.GetAbbreviatedDayName(d.DayOfWeek),
                    Value = entries,
                    LineValue = exits
                });
            }
            return new ChartComposedResponse { Data = data };
        }

        public async Task<ChartLineResponse> GetLowStockAlertsByDayAsync(int days)
        {
            var end = DateTime.UtcNow.Date;
            var start = end.AddDays(-days);
            var culture = new CultureInfo("es-ES");
            var data = new List<ChartDataPointResponse>();
            for (var d = start; d <= end; d = d.AddDays(1))
            {
                data.Add(new ChartDataPointResponse
                {
                    Label = culture.DateTimeFormat.GetAbbreviatedDayName(d.DayOfWeek),
                    Date = d.ToString("yyyy-MM-dd"),
                    Value = 0
                });
            }
            return new ChartLineResponse { Data = data };
        }

        public async Task<DashboardGrossSalesProfitResponse> GetGrossSalesProfitKpiAsync(string? period)
        {
            var (grossTotal, _, orderCount, from, to, normalized) = await ComputeSalesWithoutReturnsProfitAsync(period);
            return new DashboardGrossSalesProfitResponse
            {
                Period = normalized,
                FromUtc = from,
                ToUtcExclusive = to,
                GrossProfit = grossTotal,
                OrderCount = orderCount,
            };
        }

        public async Task<DashboardNetSalesProfitResponse> GetNetSalesProfitKpiAsync(string? period)
        {
            var (_, netProfit, orderCount, from, to, normalized) = await ComputeSalesWithoutReturnsProfitAsync(period);
            return new DashboardNetSalesProfitResponse
            {
                Period = normalized,
                FromUtc = from,
                ToUtcExclusive = to,
                NetProfit = netProfit,
                OrderCount = orderCount,
            };
        }

        /// <summary>
        /// Ventas confirmadas sin ninguna devolución registrada; ingreso = suma de totales;
        /// ganancia neta KPI = ingreso − costo de ventas (UnitCost × cantidad por línea).
        /// </summary>
        private async Task<(decimal grossTotal, decimal netProfit, int orderCount, DateTime fromUtc, DateTime toUtcExclusive, string normalizedPeriod)>
            ComputeSalesWithoutReturnsProfitAsync(string? period)
        {
            var (fromUtc, toUtcExclusive, normalizedPeriod) = ResolveSalesProfitPeriodUtc(period);
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                return (0m, 0m, 0, fromUtc, toUtcExclusive, normalizedPeriod);

            var decimals = _inventorySettings.PriceRoundingDecimals;

            var rows = await _context.SaleOrders
                .AsNoTracking()
                .Where(s => s.Status == SaleOrderStatus.confirmed)
                .Where(s => s.CreatedAt >= fromUtc && s.CreatedAt < toUtcExclusive)
                .Where(s => !s.Returns.Any())
                .Select(s => new
                {
                    s.Total,
                    Cogs = s.Items.Sum(i => i.UnitCost * i.Quantity),
                })
                .ToListAsync();

            var grossTotal = Math.Round(rows.Sum(x => x.Total), decimals, MidpointRounding.AwayFromZero);
            var cogs = Math.Round(rows.Sum(x => x.Cogs), decimals, MidpointRounding.AwayFromZero);
            var netProfit = Math.Round(grossTotal - cogs, decimals, MidpointRounding.AwayFromZero);
            return (grossTotal, netProfit, rows.Count, fromUtc, toUtcExclusive, normalizedPeriod);
        }

        /// <summary>Rango en UTC según period: day, week, month, year (default month).</summary>
        private static (DateTime fromUtc, DateTime toUtcExclusive, string normalizedPeriod) ResolveSalesProfitPeriodUtc(string? period)
        {
            var p = (period ?? "month").Trim().ToLowerInvariant();
            if (p != "day" && p != "week" && p != "month" && p != "year")
                p = "month";

            var now = DateTime.UtcNow;
            return p switch
            {
                "day" => WithLabel(CubaBusinessCalendar.GetCubaCalendarDayRangeUtc(now), p),
                "week" => WithLabel(CubaBusinessCalendar.GetCubaWeekRangeUtc(now), p),
                "year" => WithLabel(CubaBusinessCalendar.GetCubaYearRangeUtc(now), p),
                _ => WithLabel(CubaBusinessCalendar.GetCubaMonthRangeUtc(now), "month"),
            };
        }

        private static (DateTime fromUtc, DateTime toUtcExclusive, string normalizedPeriod) WithLabel(
            (DateTime fromUtc, DateTime toUtcExclusive) range, string normalizedPeriod) =>
            (range.fromUtc, range.toUtcExclusive, normalizedPeriod);

        public async Task<ProductStatsResponse> GetProductStatsAsync(DateTime? from, DateTime? to)
        {
            var minStock = _inventorySettings.DefaultMinimumStock;
            var totalProducts = await _uow.ProductRepository.GetAll().Where(p => !p.IsDeleted).CountAsync();
            var inventories = _uow.InventoryRepository.GetAllIncluding(i => i.Product)
                .Where(i => i.Product != null && !i.Product.IsDeleted);
            var inventoryValue = await inventories.SumAsync(i => i.CurrentStock * (i.Product != null ? i.Product.Costo : 0));
            var criticalStockCount = await inventories.CountAsync(i => i.CurrentStock <= minStock);
            var (cubaTodayStartUtc, cubaTodayEndExclusiveUtc) = CubaBusinessCalendar.GetCubaCalendarDayRangeUtc(DateTime.UtcNow);
            var movementsToday = await _uow.InventoryMovementRepository.GetAll()
                .CountAsync(m => m.CreatedAt >= cubaTodayStartUtc && m.CreatedAt < cubaTodayEndExclusiveUtc);

            return new ProductStatsResponse
            {
                TotalProducts = totalProducts,
                TotalProductsTrend = 12,
                InventoryValue = inventoryValue,
                InventoryValueTrend = 4,
                CriticalStockCount = criticalStockCount,
                CriticalStockTrend = -2,
                MovementsToday = movementsToday,
                MovementsTodayTrend = 8
            };
        }

        public async Task<ChartLineResponse> GetProductPerformanceAsync(int days, DateTime? from, DateTime? to)
        {
            return await GetInventoryFlowAsync(days, from, to);
        }

        public async Task<ChartDonutResponse> GetStockByCategoryAsync()
        {
            return await GetCategoryDistributionAsync();
        }

        public async Task<CategoryStatsResponse> GetCategoryStatsAsync()
        {
            var categories = _uow.ProductCategoryRepository.GetAll();
            var totalCategories = await categories.CountAsync();
            var totalItems = await _uow.ProductRepository.GetAll().Where(p => !p.IsDeleted).CountAsync();
            var lastEdited = await categories.OrderByDescending(c => c.ModifiedAt).FirstOrDefaultAsync();
            var lastEditedAgo = lastEdited == null ? "" : FormatTimeAgo(DateTime.UtcNow - lastEdited.ModifiedAt);
            var mostActive = await _uow.ProductRepository.GetAll()
                .Where(p => !p.IsDeleted && p.CategoryId != null)
                .GroupBy(p => p.CategoryId)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();
            string mostActiveName = "—";
            if (mostActive.HasValue && mostActive.Value > 0)
            {
                var cat = await _uow.ProductCategoryRepository.FirstOrDefaultAsync(c => c.Id == mostActive.Value);
                if (cat != null) mostActiveName = cat.Name;
            }

            return new CategoryStatsResponse
            {
                TotalCategories = totalCategories,
                MostActiveCategoryName = mostActiveName,
                LastEditedAgo = lastEditedAgo,
                TotalItems = totalItems
            };
        }

        public async Task<ChartLineResponse> GetCategoryItemDistributionAsync(string? period, int? days)
        {
            var byCategory = await _uow.ProductRepository.GetAll()
                .Where(p => !p.IsDeleted)
                .GroupBy(p => p.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToListAsync();
            var categoryList = await _uow.ProductCategoryRepository.GetAll()
                .Select(c => new { c.Id, c.Name }).ToListAsync();
            var categories = categoryList.ToDictionary(c => c.Id, c => c.Name.Length > 3 ? c.Name.Substring(0, 3) : c.Name);
            var data = byCategory.Select(x => new ChartDataPointResponse
            {
                Label = x.CategoryId.HasValue && categories.TryGetValue(x.CategoryId.Value, out var name) ? name : "Sin categoría",
                Value = x.Count
            }).ToList();
            return new ChartLineResponse { Data = data };
        }

        public async Task<ChartDonutResponse> GetCategoryStorageUsageAsync()
        {
            var byCategory = await _uow.ProductRepository.GetAllIncluding(p => p.Category)
                .Where(p => !p.IsDeleted && p.Category != null)
                .GroupBy(p => p.Category!.Name)
                .Select(g => new ChartDonutItemResponse { Name = g.Key ?? "Otros", Value = g.Count() })
                .ToListAsync();
            return new ChartDonutResponse { Data = byCategory };
        }

        public async Task<SupplierStatsResponse> GetSupplierStatsAsync(DateTime? from, DateTime? to)
        {
            var totalSuppliers = await _uow.SupplierRepository.GetAll().CountAsync();
            var movements = _uow.InventoryMovementRepository.GetAll().Where(m => m.SupplierId != null);
            var activeOrders = await movements.CountAsync();
            return new SupplierStatsResponse
            {
                TotalSuppliers = totalSuppliers,
                TotalSuppliersTrend = 12,
                ActiveOrders = activeOrders,
                ActiveOrdersTrend = 8,
                CompliancePercent = 94,
                ComplianceTrend = 3,
                MonthlyExpenses = 12400,
                MonthlyExpensesTrend = 15
            };
        }

        public async Task<ChartLineResponse> GetSupplierDeliveryFrequencyAsync(int? days, DateTime? from, DateTime? to)
        {
            return await GetInventoryFlowAsync(days ?? 7, from, to);
        }

        public async Task<ChartLineResponse> GetSupplierDeliveryTimelineAsync(int? days, DateTime? from, DateTime? to)
        {
            return await GetInventoryFlowAsync(days ?? 7, from, to);
        }

        public async Task<ChartDonutResponse> GetSupplierCategoryDistributionAsync()
        {
            var byCategory = await _uow.InventoryMovementRepository.GetAll()
                .Where(m => m.SupplierId != null)
                .GroupBy(m => m.SupplierId)
                .Select(g => new { SupplierId = g.Key, Count = g.Count() })
                .ToListAsync();
            var data = new List<ChartDonutItemResponse>();
            foreach (var item in byCategory.Take(5))
            {
                if (item.SupplierId == null) continue;
                var s = await _uow.SupplierRepository.FirstOrDefaultAsync(x => x.Id == item.SupplierId.Value);
                data.Add(new ChartDonutItemResponse { Name = s?.Name ?? "Proveedor", Value = item.Count });
            }
            return new ChartDonutResponse { Data = data };
        }

        public async Task<InventoryStatsResponse> GetInventoryStatsAsync()
        {
            var minStock = _inventorySettings.DefaultMinimumStock;
            var inventories = _uow.InventoryRepository.GetAllIncluding(i => i.Product)
                .Where(i => i.Product != null && !i.Product.IsDeleted);
            var totalValue = await inventories.SumAsync(i => i.CurrentStock * (i.Product != null ? i.Product.Costo : 0));
            var lowStockCount = await inventories.CountAsync(i => i.CurrentStock <= minStock);
            var (cubaTodayStartUtc, cubaTodayEndExclusiveUtc) = CubaBusinessCalendar.GetCubaCalendarDayRangeUtc(DateTime.UtcNow);
            var movementsCount = await _uow.InventoryMovementRepository.GetAll()
                .CountAsync(m => m.CreatedAt >= cubaTodayStartUtc && m.CreatedAt < cubaTodayEndExclusiveUtc);
            var productsCount = await _uow.ProductRepository.GetAll().Where(p => !p.IsDeleted).CountAsync();

            return new InventoryStatsResponse
            {
                TotalValue = totalValue,
                TotalValueTrend = 12,
                LowStockCount = lowStockCount,
                LowStockNewToday = 0,
                MovementsCount = movementsCount,
                MovementsTrend = -5,
                ProductsCount = productsCount,
                ProductsNewCount = 0
            };
        }

        public async Task<ChartLineResponse> GetInventoryFlowAsync(int days)
        {
            return await GetInventoryFlowAsync(days, null, null);
        }

        public async Task<ChartLineResponse> GetStockByLocationAsync()
        {
            var byLocation = await _uow.InventoryRepository.GetAllIncluding(i => i.Location)
                .Where(i => i.Location != null)
                .GroupBy(i => i.LocationId)
                .Select(g => new { LocationId = g.Key, Total = g.Sum(i => i.CurrentStock) })
                .ToListAsync();
            var locIds = byLocation.Select(x => x.LocationId).ToList();
            var locations = await _uow.LocationRepository.GetAll()
                .Where(l => locIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, l => l.Name);
            var data = byLocation.Select(x => new ChartDataPointResponse
            {
                Label = locations.TryGetValue(x.LocationId, out var name) ? name : "—",
                Value = x.Total
            }).ToList();
            return new ChartLineResponse { Data = data };
        }

        public async Task<ChartDonutResponse> GetInventoryCategoryDistributionAsync()
        {
            return await GetCategoryDistributionAsync();
        }

        public async Task<MovementStatsResponse> GetMovementStatsAsync(DateTime? from, DateTime? to, bool todayOnly)
        {
            var query = _uow.InventoryMovementRepository.GetAll();
            if (todayOnly)
            {
                var (cubaTodayStartUtc, cubaTodayEndExclusiveUtc) = CubaBusinessCalendar.GetCubaCalendarDayRangeUtc(DateTime.UtcNow);
                query = query.Where(m => m.CreatedAt >= cubaTodayStartUtc && m.CreatedAt < cubaTodayEndExclusiveUtc);
            }
            else if (from.HasValue && to.HasValue)
            {
                var f = from.Value.Date;
                var t = to.Value.Date;
                var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(f, DateTimeKind.Unspecified), CubaBusinessCalendar.CubaTimeZone);
                var endUtcExclusive = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(t.AddDays(1), DateTimeKind.Unspecified), CubaBusinessCalendar.CubaTimeZone);
                query = query.Where(m => m.CreatedAt >= startUtc && m.CreatedAt < endUtcExclusive);
            }

            var total = await query.CountAsync();
            var entries = await query.CountAsync(m => m.Type == InventoryMovementType.entry);
            var exits = await query.CountAsync(m => m.Type == InventoryMovementType.exit);
            var adjustments = await query.CountAsync(m => m.Type == InventoryMovementType.adjustment);

            return new MovementStatsResponse
            {
                TotalMovements = total,
                TotalMovementsTrend = 12,
                EntriesCount = entries,
                EntriesTrend = 5,
                ExitsCount = exits,
                ExitsTrend = -2,
                AdjustmentsCount = adjustments,
                AdjustmentsLabel = "Estable"
            };
        }

        public async Task<ChartLineResponse> GetMovementFlowAsync(int days)
        {
            return await GetInventoryFlowAsync(days, null, null);
        }

        public async Task<ChartComposedResponse> GetMovementFlowWithCumulativeAsync(int days)
        {
            var (startUtc, endUtcExclusive, loopStartCuba, loopEndCuba) = CubaBusinessCalendar.ResolveMovementFlowRange(days, null, null);
            var movements = await _uow.InventoryMovementRepository.GetAll()
                .Where(m => m.CreatedAt >= startUtc && m.CreatedAt < endUtcExclusive)
                .ToListAsync();
            var byDay = movements
                .GroupBy(m => TimeZoneInfo.ConvertTimeFromUtc(m.CreatedAt, CubaBusinessCalendar.CubaTimeZone).Date)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());
            var culture = new CultureInfo("es-ES");
            var data = new List<ChartComposedPointResponse>();
            var cumulative = 0;
            for (var d = loopStartCuba; d <= loopEndCuba; d = d.AddDays(1))
            {
                var count = byDay.TryGetValue(d, out var c) ? c : 0;
                cumulative += count;
                data.Add(new ChartComposedPointResponse
                {
                    Label = culture.DateTimeFormat.GetAbbreviatedDayName(d.DayOfWeek),
                    Value = count,
                    LineValue = cumulative
                });
            }
            return new ChartComposedResponse { Data = data };
        }

        public async Task<ChartDonutResponse> GetMovementDistributionByTypeAsync()
        {
            var entries = await _uow.InventoryMovementRepository.GetAll().CountAsync(m => m.Type == InventoryMovementType.entry);
            var exits = await _uow.InventoryMovementRepository.GetAll().CountAsync(m => m.Type == InventoryMovementType.exit);
            var adjustments = await _uow.InventoryMovementRepository.GetAll().CountAsync(m => m.Type == InventoryMovementType.adjustment);
            var total = entries + exits + adjustments;
            var data = new List<ChartDonutItemResponse>();
            if (total > 0)
            {
                data.Add(new ChartDonutItemResponse { Name = "Entradas", Value = Math.Round(100m * entries / total, 0) });
                data.Add(new ChartDonutItemResponse { Name = "Salidas", Value = Math.Round(100m * exits / total, 0) });
                data.Add(new ChartDonutItemResponse { Name = "Ajustes", Value = Math.Round(100m * adjustments / total, 0) });
            }
            return new ChartDonutResponse { Data = data };
        }

        private static string FormatTimeAgo(TimeSpan span)
        {
            if (span.TotalMinutes < 1) return "ahora";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h";
            if (span.TotalDays < 30) return $"{(int)span.TotalDays}d";
            return $"{(int)(span.TotalDays / 30)}mes";
        }
    }
}
