using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class CatalogMetricsTrackingService : ICatalogMetricsTrackingService
    {
        private const int MaxBatchEvents = 100;

        private static readonly HashSet<string> AllowedTrafficSources = new(StringComparer.OrdinalIgnoreCase)
        {
            "direct", "search", "social", "external",
        };

        private readonly CoreDbContext _context;

        public CatalogMetricsTrackingService(CoreDbContext context)
        {
            _context = context;
        }

        public async Task AppendPublicEventsAsync(CatalogMetricsBatchRequest request, int? authenticatedUserId, CancellationToken cancellationToken = default)
        {
            if (request.Events.Count > MaxBatchEvents)
            {
                throw new BaseBadRequestException
                {
                    CustomCode = 400470,
                    CustomMessage = $"Se permiten como máximo {MaxBatchEvents} eventos por solicitud.",
                };
            }

            var location = await _context.Locations
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == request.LocationId, cancellationToken);
            if (location == null)
            {
                throw new BaseBadRequestException
                {
                    CustomCode = 400471,
                    CustomMessage = "La ubicación del catálogo no es válida.",
                };
            }

            var orgId = location.OrganizationId;
            var now = DateTime.UtcNow;
            var rows = new List<MetricsEvent>();

            var needsVisitor = request.Events.Any(e => RequiresVisitorIdentity((e.Type ?? string.Empty).Trim().ToLowerInvariant()));
            if (needsVisitor && (authenticatedUserId is not int authId || authId <= 0) && string.IsNullOrWhiteSpace(request.SessionId))
            {
                throw new BaseBadRequestException
                {
                    CustomCode = 400478,
                    CustomMessage = "Indique sessionId o autentíquese para registrar eventos de catálogo.",
                };
            }

            foreach (var ev in request.Events)
            {
                var type = (ev.Type ?? string.Empty).Trim().ToLowerInvariant();
                var catalogLocationId = ev.CatalogId ?? request.LocationId;

                if (catalogLocationId != request.LocationId)
                {
                    var catalogLoc = await _context.Locations
                        .IgnoreQueryFilters()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(l => l.Id == catalogLocationId, cancellationToken);
                    if (catalogLoc == null || catalogLoc.OrganizationId != orgId)
                    {
                        throw new BaseBadRequestException
                        {
                            CustomCode = 400472,
                            CustomMessage = "El catálogo indicado no pertenece al mismo negocio que la ubicación.",
                        };
                    }
                }

                var occurredAt = ev.OccurredAt.HasValue ? NormalizeToUtc(ev.OccurredAt.Value) : now;

                switch (type)
                {
                    case MetricsEventTypes.CatalogVisit:
                        rows.Add(CreateRow(orgId, catalogLocationId, type, occurredAt, request.SessionId, authenticatedUserId,
                            trafficSource: NormalizeTrafficSource(ev.TrafficSource),
                            durationSeconds: ev.DurationSeconds));
                        break;

                    case MetricsEventTypes.ProductView:
                    case MetricsEventTypes.AddToCart:
                        if (!ev.ProductId.HasValue || ev.ProductId.Value <= 0)
                        {
                            throw new BaseBadRequestException { CustomCode = 400473, CustomMessage = "productId es obligatorio para este evento." };
                        }

                        var pvProductId = ev.ProductId.Value;
                        await EnsureProductInOrgAsync(pvProductId, orgId, cancellationToken);
                        rows.Add(CreateRow(orgId, catalogLocationId, type, occurredAt, request.SessionId, authenticatedUserId,
                            productId: pvProductId));
                        break;

                    case MetricsEventTypes.CatalogSearch:
                        if (string.IsNullOrWhiteSpace(ev.SearchTerm))
                        {
                            throw new BaseBadRequestException { CustomCode = 400474, CustomMessage = "searchTerm es obligatorio para búsquedas." };
                        }

                        rows.Add(CreateRow(orgId, catalogLocationId, type, occurredAt, request.SessionId, authenticatedUserId,
                            searchTerm: Truncate(ev.SearchTerm.Trim(), 512)));
                        break;

                    case MetricsEventTypes.ProductFavorited:
                        if (authenticatedUserId is not int uid || uid <= 0)
                        {
                            throw new BaseBadRequestException
                            {
                                CustomCode = 400475,
                                CustomMessage = "Debe iniciar sesión para registrar favoritos.",
                            };
                        }

                        if (!ev.ProductId.HasValue || ev.ProductId.Value <= 0)
                        {
                            throw new BaseBadRequestException { CustomCode = 400473, CustomMessage = "productId es obligatorio para este evento." };
                        }

                        var favPid = ev.ProductId.Value;
                        await EnsureProductInOrgAsync(favPid, orgId, cancellationToken);
                        rows.Add(CreateRow(orgId, catalogLocationId, type, occurredAt, sessionId: null, userId: uid, productId: favPid));
                        break;

                    case MetricsEventTypes.CartAbandoned:
                        rows.Add(CreateRow(orgId, catalogLocationId, type, occurredAt, request.SessionId, authenticatedUserId));
                        break;

                    default:
                        throw new BaseBadRequestException
                        {
                            CustomCode = 400476,
                            CustomMessage = "Tipo de evento de métricas no reconocido.",
                        };
                }
            }

            foreach (var row in rows)
            {
                _context.MetricsEvents.Add(row);
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public void StagePurchaseCompletedEvents(SaleOrder order)
        {
            if (order.Items == null || order.Items.Count == 0)
                return;

            var occurredAt = DateTime.UtcNow;
            foreach (var item in order.Items)
            {
                _context.MetricsEvents.Add(new MetricsEvent
                {
                    OrganizationId = order.OrganizationId,
                    LocationId = order.LocationId,
                    EventType = MetricsEventTypes.PurchaseCompleted,
                    OccurredAt = occurredAt,
                    UserId = order.UserId,
                    SessionId = null,
                    ProductId = item.ProductId,
                    SaleOrderId = order.Id,
                });
            }
        }

        private static MetricsEvent CreateRow(
            int organizationId,
            int locationId,
            string eventType,
            DateTime occurredAt,
            string? sessionId,
            int? userId,
            int? productId = null,
            string? trafficSource = null,
            string? searchTerm = null,
            int? durationSeconds = null) =>
            new MetricsEvent
            {
                OrganizationId = organizationId,
                LocationId = locationId,
                EventType = eventType,
                OccurredAt = occurredAt,
                UserId = userId,
                SessionId = TruncateSession(sessionId),
                ProductId = productId,
                TrafficSource = trafficSource,
                SearchTerm = searchTerm,
                DurationSeconds = durationSeconds,
            };

        private async Task EnsureProductInOrgAsync(int productId, int organizationId, CancellationToken cancellationToken)
        {
            var ok = await _context.Products
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Id == productId && p.OrganizationId == organizationId, cancellationToken);
            if (!ok)
            {
                throw new BaseBadRequestException
                {
                    CustomCode = 400477,
                    CustomMessage = "El producto no pertenece a este catálogo.",
                };
            }
        }

        private static string? NormalizeTrafficSource(string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return "direct";
            var s = source.Trim().ToLowerInvariant();
            return AllowedTrafficSources.Contains(s) ? s : "direct";
        }

        private static string? TruncateSession(string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return null;
            return Truncate(sessionId.Trim(), 128);
        }

        private static string Truncate(string value, int maxLen) =>
            value.Length <= maxLen ? value : value.Substring(0, maxLen);

        private static DateTime NormalizeToUtc(DateTime dt) =>
            dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            };

        private static bool RequiresVisitorIdentity(string type) =>
            type is MetricsEventTypes.CatalogVisit
                or MetricsEventTypes.ProductView
                or MetricsEventTypes.AddToCart
                or MetricsEventTypes.CatalogSearch
                or MetricsEventTypes.CartAbandoned;
    }
}
