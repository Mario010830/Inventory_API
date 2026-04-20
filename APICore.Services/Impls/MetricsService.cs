using APICore.Common.Constants;
using APICore.Common.DTO.Response;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class MetricsService : IMetricsService
    {
        private static readonly TimeSpan CartAbandonmentWindow = TimeSpan.FromHours(24);

        private readonly CoreDbContext _context;
        private readonly ICurrentUserContextAccessor _contextAccessor;

        public MetricsService(CoreDbContext context, ICurrentUserContextAccessor contextAccessor)
        {
            _context = context;
            _contextAccessor = contextAccessor;
        }

        public async Task<MetricsTrafficResponse> GetTrafficAsync(int businessId, string? period, CancellationToken cancellationToken = default)
        {
            EnsureAccess(businessId);
            await EnsureOrganizationExistsAsync(businessId, cancellationToken);

            var (from, to, normalized) = ResolvePeriod(period);
            var q = BaseMetricsQuery(businessId, from, to);

            var visitQuery = q.Where(e => e.EventType == MetricsEventTypes.CatalogVisit);
            var totalVisits = await visitQuery.CountAsync(cancellationToken);

            var visitRows = await visitQuery
                .Select(e => new { e.UserId, e.SessionId })
                .ToListAsync(cancellationToken);
            var visitKeys = visitRows
                .Select(r => VisitorKeyFromParts(r.UserId, r.SessionId))
                .Where(k => k != null)
                .Cast<string>()
                .ToList();
            var uniqueVisitors = visitKeys.Distinct().Count();

            var productViewRows = await q
                .Where(e => e.EventType == MetricsEventTypes.ProductView)
                .Select(e => new { e.UserId, e.SessionId })
                .ToListAsync(cancellationToken);
            var viewerSet = new HashSet<string>(
                productViewRows
                    .Select(r => VisitorKeyFromParts(r.UserId, r.SessionId))
                    .OfType<string>());

            var bounceVisitors = visitKeys
                .Distinct()
                .Count(k => !viewerSet.Contains(k));

            var bounceRatePercent = uniqueVisitors == 0 ? 0 : 100.0 * bounceVisitors / uniqueVisitors;

            var durations = await visitQuery
                .Where(e => e.DurationSeconds != null && e.DurationSeconds > 0)
                .Select(e => e.DurationSeconds!.Value)
                .ToListAsync(cancellationToken);
            double? avgTime = durations.Count == 0 ? null : durations.Average();

            var sourceGroups = await visitQuery
                .GroupBy(e => e.TrafficSource ?? "unknown")
                .Select(g => new { Source = g.Key, Cnt = g.Count() })
                .ToListAsync(cancellationToken);
            var sourceTotal = sourceGroups.Sum(x => x.Cnt);
            var trafficSources = sourceGroups
                .Select(x => new MetricsTrafficSourceBreakdownResponse
                {
                    Source = x.Source,
                    Percent = sourceTotal == 0 ? 0 : Math.Round(100.0 * x.Cnt / sourceTotal, 2),
                })
                .OrderByDescending(x => x.Percent)
                .ToList();

            var searchTermRows = await q
                .Where(e => e.EventType == MetricsEventTypes.CatalogSearch && e.SearchTerm != null && e.SearchTerm != "")
                .Select(e => e.SearchTerm)
                .ToListAsync(cancellationToken);
            var topSearchTerms = searchTermRows
                .Select(t => t!.Trim().ToLowerInvariant())
                .GroupBy(t => t)
                .Select(g => new MetricsSearchTermResponse { Term = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(20)
                .ToList();

            return new MetricsTrafficResponse
            {
                FromUtc = from,
                ToUtc = to,
                Period = normalized,
                TotalVisits = totalVisits,
                UniqueVisitors = uniqueVisitors,
                BounceRatePercent = Math.Round(bounceRatePercent, 2),
                AvgTimeOnCatalogSeconds = avgTime == null ? null : Math.Round(avgTime.Value, 2),
                TrafficSources = trafficSources,
                TopSearchTerms = topSearchTerms,
            };
        }

        public async Task<MetricsProductsResponse> GetProductsAsync(int businessId, string? period, CancellationToken cancellationToken = default)
        {
            EnsureAccess(businessId);
            await EnsureOrganizationExistsAsync(businessId, cancellationToken);

            var (from, to, normalized) = ResolvePeriod(period);
            var q = BaseMetricsQuery(businessId, from, to);

            var views = await q
                .Where(e => e.EventType == MetricsEventTypes.ProductView && e.ProductId != null)
                .GroupBy(e => e.ProductId!.Value)
                .Select(g => new { ProductId = g.Key, Cnt = g.Count() })
                .ToListAsync(cancellationToken);

            var favorites = await q
                .Where(e => e.EventType == MetricsEventTypes.ProductFavorited && e.ProductId != null)
                .GroupBy(e => e.ProductId!.Value)
                .Select(g => new { ProductId = g.Key, Cnt = g.Count() })
                .ToListAsync(cancellationToken);

            var adds = await q
                .Where(e => e.EventType == MetricsEventTypes.AddToCart && e.ProductId != null)
                .GroupBy(e => e.ProductId!.Value)
                .Select(g => new { ProductId = g.Key, Cnt = g.Count() })
                .ToListAsync(cancellationToken);

            var productIds = views.Select(v => v.ProductId)
                .Union(favorites.Select(f => f.ProductId))
                .Union(adds.Select(a => a.ProductId))
                .Distinct()
                .ToList();

            var names = await ProductNamesAsync(businessId, productIds, cancellationToken);

            var mostViewed = views
                .OrderByDescending(x => x.Cnt)
                .Take(20)
                .Select(x => new MetricsProductViewsResponse
                {
                    ProductId = x.ProductId,
                    Name = names.GetValueOrDefault(x.ProductId, string.Empty),
                    ViewCount = x.Cnt,
                })
                .ToList();

            var topFav = favorites
                .OrderByDescending(x => x.Cnt)
                .Take(20)
                .Select(x => new MetricsProductFavoritesResponse
                {
                    ProductId = x.ProductId,
                    Name = names.GetValueOrDefault(x.ProductId, string.Empty),
                    FavoriteCount = x.Cnt,
                })
                .ToList();

            var viewDict = views.ToDictionary(x => x.ProductId, x => x.Cnt);
            var addDict = adds.ToDictionary(x => x.ProductId, x => x.Cnt);
            var viewToCart = productIds
                .Select(pid =>
                {
                    var vc = viewDict.GetValueOrDefault(pid);
                    var ac = addDict.GetValueOrDefault(pid);
                    var rate = vc == 0 ? 0 : Math.Min(100, Math.Round(100.0 * ac / vc, 2));
                    return new MetricsProductViewToCartResponse
                    {
                        ProductId = pid,
                        Name = names.GetValueOrDefault(pid, string.Empty),
                        ViewCount = vc,
                        AddToCartCount = ac,
                        ViewToCartRatePercent = rate,
                    };
                })
                .OrderByDescending(x => x.ViewCount)
                .ToList();

            var soldIds = await _context.SaleOrderItems
                .AsNoTracking()
                .Where(i => i.SaleOrder != null
                    && i.SaleOrder.OrganizationId == businessId
                    && i.SaleOrder.Status == SaleOrderStatus.confirmed
                    && i.SaleOrder.ModifiedAt >= from
                    && i.SaleOrder.ModifiedAt <= to)
                .Select(i => i.ProductId)
                .Distinct()
                .ToListAsync(cancellationToken);
            var soldSet = new HashSet<int>(soldIds);

            var noSales = await _context.Products
                .AsNoTracking()
                .Where(p => p.OrganizationId == businessId && p.IsForSale && p.IsAvailable && !p.IsDeleted && !soldSet.Contains(p.Id))
                .OrderBy(p => p.Name)
                .Select(p => new MetricsProductNoSalesResponse { ProductId = p.Id, Name = p.Name })
                .Take(100)
                .ToListAsync(cancellationToken);

            return new MetricsProductsResponse
            {
                FromUtc = from,
                ToUtc = to,
                Period = normalized,
                MostViewedProducts = mostViewed,
                TopFavorited = topFav,
                ProductsWithNoSales = noSales,
                ViewToCartRate = viewToCart,
            };
        }

        public async Task<MetricsSalesResponse> GetSalesAsync(int businessId, string? period, CancellationToken cancellationToken = default)
        {
            EnsureAccess(businessId);
            await EnsureOrganizationExistsAsync(businessId, cancellationToken);

            var (from, to, normalized) = ResolvePeriod(period);
            var q = BaseMetricsQuery(businessId, from, to);

            var orders = await _context.SaleOrders
                .AsNoTracking()
                .Where(o => o.OrganizationId == businessId
                    && o.Status == SaleOrderStatus.confirmed
                    && o.ModifiedAt >= from
                    && o.ModifiedAt <= to)
                .ToListAsync(cancellationToken);

            var totalRevenue = orders.Sum(o => o.Total);
            var totalOrders = orders.Count;
            var avgOrderValue = totalOrders == 0 ? 0 : Math.Round(totalRevenue / totalOrders, 2);

            var abandonmentPercent = await ComputeCartAbandonmentPercentAsync(businessId, from, to, cancellationToken);

            var visits = await q.CountAsync(e => e.EventType == MetricsEventTypes.CatalogVisit, cancellationToken);
            var productViews = await q.CountAsync(e => e.EventType == MetricsEventTypes.ProductView, cancellationToken);
            var addedToCart = await q.CountAsync(e => e.EventType == MetricsEventTypes.AddToCart, cancellationToken);
            var completed = await q
                .Where(e => e.EventType == MetricsEventTypes.PurchaseCompleted && e.SaleOrderId != null)
                .Select(e => e.SaleOrderId!.Value)
                .Distinct()
                .CountAsync(cancellationToken);

            return new MetricsSalesResponse
            {
                FromUtc = from,
                ToUtc = to,
                Period = normalized,
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                AvgOrderValue = avgOrderValue,
                CartAbandonmentRatePercent = abandonmentPercent,
                ConversionFunnel = new MetricsConversionFunnelResponse
                {
                    Visits = visits,
                    ProductViews = productViews,
                    AddedToCart = addedToCart,
                    Completed = completed,
                },
            };
        }

        public async Task<MetricsCustomersResponse> GetCustomersAsync(int businessId, string? period, CancellationToken cancellationToken = default)
        {
            EnsureAccess(businessId);
            await EnsureOrganizationExistsAsync(businessId, cancellationToken);

            var (from, to, normalized) = ResolvePeriod(period);

            var ordersInPeriod = await _context.SaleOrders
                .AsNoTracking()
                .Where(o => o.OrganizationId == businessId
                    && o.Status == SaleOrderStatus.confirmed
                    && o.ModifiedAt >= from
                    && o.ModifiedAt <= to)
                .Select(o => new { o.ContactId, o.UserId })
                .ToListAsync(cancellationToken);

            var buyerKeysInPeriod = ordersInPeriod
                .Select(o => BuyerKey(o.ContactId, o.UserId))
                .Where(k => k != null)
                .Cast<string>()
                .Distinct()
                .ToList();

            var priorOrders = await _context.SaleOrders
                .AsNoTracking()
                .Where(o => o.OrganizationId == businessId
                    && o.Status == SaleOrderStatus.confirmed
                    && o.ModifiedAt < from)
                .Select(o => new { o.ContactId, o.UserId })
                .ToListAsync(cancellationToken);

            var priorBuyerKeys = new HashSet<string>(
                priorOrders.Select(o => BuyerKey(o.ContactId, o.UserId)).OfType<string>());

            var newBuyers = buyerKeysInPeriod.Count(k => !priorBuyerKeys.Contains(k));
            var returningBuyers = buyerKeysInPeriod.Count(k => priorBuyerKeys.Contains(k));

            return new MetricsCustomersResponse
            {
                FromUtc = from,
                ToUtc = to,
                Period = normalized,
                NewBuyers = newBuyers,
                ReturningBuyers = returningBuyers,
                RatingsAverage = null,
                RatingsDistribution = new List<MetricsRatingBucketResponse>(),
                Reviews = new List<MetricsReviewResponse>(),
            };
        }

        private IQueryable<MetricsEvent> BaseMetricsQuery(int organizationId, DateTime fromUtc, DateTime toUtc) =>
            _context.MetricsEvents
                .AsNoTracking()
                .Where(e => e.OrganizationId == organizationId && e.OccurredAt >= fromUtc && e.OccurredAt <= toUtc);

        private void EnsureAccess(int businessId)
        {
            var ctx = _contextAccessor.GetCurrent();
            if (ctx == null)
            {
                throw new BaseUnauthorizedException
                {
                    CustomCode = 401001,
                    CustomMessage = "Debe iniciar sesión para consultar métricas.",
                };
            }

            if (!ctx.IsSuperAdmin && ctx.OrganizationId != businessId)
            {
                throw new BaseForbiddenException
                {
                    CustomCode = 403450,
                    CustomMessage = "No tiene acceso a las métricas de esta organización.",
                };
            }
        }

        private async Task EnsureOrganizationExistsAsync(int businessId, CancellationToken cancellationToken)
        {
            var exists = await _context.Organizations
                .IgnoreQueryFilters()
                .AnyAsync(o => o.Id == businessId, cancellationToken);
            if (!exists)
                throw new OrganizationNotFoundException();
        }

        private static (DateTime fromUtc, DateTime toUtc, string normalized) ResolvePeriod(string? period)
        {
            var toUtc = DateTime.UtcNow;
            var days = period?.Trim().ToLowerInvariant() switch
            {
                "7d" => 7,
                "90d" => 90,
                "30d" => 30,
                _ => 30,
            };
            var normalized = period?.Trim().ToLowerInvariant() is "7d" or "30d" or "90d"
                ? period!.Trim().ToLowerInvariant()
                : "30d";
            return (toUtc.AddDays(-days), toUtc, normalized);
        }

        private static string? BuyerKey(int? contactId, int? userId)
        {
            if (contactId is int c && c > 0)
                return "c:" + c;
            if (userId is int u && u > 0)
                return "u:" + u;
            return null;
        }

        private async Task<Dictionary<int, string>> ProductNamesAsync(int organizationId, IList<int> productIds, CancellationToken cancellationToken)
        {
            if (productIds.Count == 0)
                return new Dictionary<int, string>();

            return await _context.Products
                .AsNoTracking()
                .Where(p => p.OrganizationId == organizationId && productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);
        }

        private async Task<double> ComputeCartAbandonmentPercentAsync(int organizationId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
        {
            var adds = await BaseMetricsQuery(organizationId, fromUtc, toUtc)
                .Where(e => e.EventType == MetricsEventTypes.AddToCart)
                .Select(e => new { e.UserId, e.SessionId, e.OccurredAt })
                .ToListAsync(cancellationToken);

            var purchases = await BaseMetricsQuery(organizationId, fromUtc, toUtc.Add(CartAbandonmentWindow))
                .Where(e => e.EventType == MetricsEventTypes.PurchaseCompleted)
                .Select(e => new { e.UserId, e.SessionId, e.OccurredAt })
                .ToListAsync(cancellationToken);

            var groups = adds
                .Select(a => VisitorKeyFromParts(a.UserId, a.SessionId))
                .Where(k => k != null)
                .Distinct()
                .ToList();

            if (groups.Count == 0)
                return 0;

            var abandoned = 0;
            foreach (var key in groups)
            {
                var keyAdds = adds
                    .Where(a => VisitorKeyFromParts(a.UserId, a.SessionId) == key)
                    .ToList();
                if (keyAdds.Count == 0)
                    continue;
                var firstAdd = keyAdds.Min(x => x.OccurredAt);
                var lastAdd = keyAdds.Max(x => x.OccurredAt);
                var windowEnd = lastAdd.Add(CartAbandonmentWindow);

                var converted = purchases.Any(p =>
                    VisitorKeyFromParts(p.UserId, p.SessionId) == key
                    && p.OccurredAt >= firstAdd
                    && p.OccurredAt <= windowEnd);

                if (!converted)
                    abandoned++;
            }

            return Math.Round(100.0 * abandoned / groups.Count, 2);
        }

        private static string? VisitorKeyFromParts(int? userId, string? sessionId)
        {
            if (userId is int u && u > 0)
                return "u:" + u;
            if (!string.IsNullOrWhiteSpace(sessionId))
                return "s:" + sessionId.Trim();
            return null;
        }
    }
}
