#nullable enable
using APICore.Common.DTO.Response;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Services;
using APICore.Services.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace APICore.Services.Impls
{
    public class PublicCatalogService : IPublicCatalogService
    {
        private readonly CoreDbContext _context;
        private readonly IInventorySettings _inventorySettings;

        public PublicCatalogService(CoreDbContext context, IInventorySettings inventorySettings)
        {
            _context = context;
            _inventorySettings = inventorySettings ?? throw new ArgumentNullException(nameof(inventorySettings));
        }

        public async Task<IEnumerable<PublicLocationResponse>> GetLocationsAsync(
            string? sortBy = null, string? sortDir = null,
            double? lat = null, double? lng = null, double? radiusKm = null,
            int? categoryId = null)
        {
            // IgnoreQueryFilters porque el usuario no está autenticado y los filtros
            // globales de multitenancy devolverían vacío (CurrentOrganizationId = -1).
            var locations = await _context.Locations
                .IgnoreQueryFilters()
                .Include(l => l.Organization)
                .Include(l => l.BusinessCategory)
                .ToListAsync();

            // Pre-computar productCount y hasPromo por location
            var locationIds = locations.Select(l => l.Id).ToList();
            var orgIds = locations.Select(l => l.OrganizationId).Distinct().ToList();

            var inventoryCounts = await _context.Inventories
                .IgnoreQueryFilters()
                .Where(i => locationIds.Contains(i.LocationId) && i.CurrentStock > 0)
                .GroupBy(i => i.LocationId)
                .Select(g => new { LocationId = g.Key, Count = g.Select(i => i.ProductId).Distinct().Count() })
                .ToDictionaryAsync(x => x.LocationId, x => x.Count);

            var elaboradoOfferCounts = await _context.ProductLocationOffers
                .IgnoreQueryFilters()
                .Where(o => locationIds.Contains(o.LocationId))
                .GroupBy(o => o.LocationId)
                .Select(g => new { LocationId = g.Key, Count = g.Select(o => o.ProductId).Distinct().Count() })
                .ToDictionaryAsync(x => x.LocationId, x => x.Count);

            var nowUtc = DateTime.UtcNow;
            var activePromoProductIds = await _context.Promotions
                .IgnoreQueryFilters()
                .Where(p => p.IsActive
                    && p.MinQuantity <= 1
                    && (!p.StartsAt.HasValue || p.StartsAt.Value <= nowUtc)
                    && (!p.EndsAt.HasValue || p.EndsAt.Value >= nowUtc))
                .Select(p => p.ProductId)
                .Distinct()
                .ToListAsync();
            var activePromoSet = activePromoProductIds.ToHashSet();

            // Productos con promo activa por location (via inventario)
            var promoByLocation = await _context.Inventories
                .IgnoreQueryFilters()
                .Where(i => locationIds.Contains(i.LocationId)
                    && i.CurrentStock > 0
                    && activePromoSet.Contains(i.ProductId))
                .Select(i => i.LocationId)
                .Distinct()
                .ToListAsync();
            var locationsWithPromo = promoByLocation.ToHashSet();

            // Usamos hora local del servidor para el cálculo de isOpenNow.
            var now = DateTime.Now;

            var result = locations.Select(l =>
            {
                var businessHours = DeserializeBusinessHours(l.BusinessHoursJson);
                var coordinates = (l.Latitude.HasValue && l.Longitude.HasValue)
                    ? new PublicLocationCoordinatesDto
                    {
                        Lat = l.Latitude.Value,
                        Lng = l.Longitude.Value
                    }
                    : null;

                inventoryCounts.TryGetValue(l.Id, out var invCount);
                elaboradoOfferCounts.TryGetValue(l.Id, out var elabCount);

                var isOpenNow = CalculateIsOpenNow(businessHours, now);

                return new PublicLocationResponse
                {
                    Id = l.Id,
                    Name = l.Name,
                    Description = l.Description,
                    OrganizationId = l.OrganizationId,
                    OrganizationName = l.Organization != null ? l.Organization.Name : string.Empty,
                    WhatsAppContact = l.WhatsAppContact,
                    PhotoUrl = l.PhotoUrl,
                    Province = l.Province,
                    Municipality = l.Municipality,
                    Street = l.Street,
                    BusinessHours = businessHours,
                    Coordinates = coordinates,
                    IsOpenNow = isOpenNow,
                    IsVerified = l.IsVerified,
                    // Configuración en BD; el front usa IsOpenNow para deshabilitar pedidos fuera de horario.
                    OffersDelivery = l.OffersDelivery,
                    OffersPickup = l.OffersPickup,
                    CreatedAt = l.CreatedAt,
                    ProductCount = invCount + elabCount,
                    HasPromo = locationsWithPromo.Contains(l.Id),
                    BusinessCategoryId = l.BusinessCategoryId,
                    BusinessCategory = l.BusinessCategory != null
                        ? new BusinessCategorySummaryDto { Name = l.BusinessCategory.Name, Icon = l.BusinessCategory.Icon }
                        : null,
                };
            }).ToList();

            // Filtro por categoría
            if (categoryId.HasValue)
                result = result.Where(r => r.BusinessCategoryId == categoryId.Value).ToList();

            // Filtro geográfico por radio (bounding box + haversine)
            if (lat.HasValue && lng.HasValue && radiusKm.HasValue && radiusKm.Value > 0)
            {
                result = result.Where(r =>
                {
                    if (r.Coordinates == null) return false;
                    var dist = HaversineKm(lat.Value, lng.Value, r.Coordinates.Lat, r.Coordinates.Lng);
                    return dist <= radiusKm.Value;
                }).ToList();
            }

            // Sorting
            var dir = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
            result = (sortBy?.ToLowerInvariant()) switch
            {
                "newest" => dir == "asc"
                    ? result.OrderBy(r => r.CreatedAt).ToList()
                    : result.OrderByDescending(r => r.CreatedAt).ToList(),
                "name" => dir == "desc"
                    ? result.OrderByDescending(r => r.Name).ToList()
                    : result.OrderBy(r => r.Name).ToList(),
                "productcount" => dir == "asc"
                    ? result.OrderBy(r => r.ProductCount).ToList()
                    : result.OrderByDescending(r => r.ProductCount).ToList(),
                _ => result.OrderBy(r => r.Name).ToList(),
            };

            return result;
        }

        public async Task<IEnumerable<PublicCatalogItemResponse>> GetCatalogByLocationAsync(int locationId)
        {
            // Traer todos los productos con IsForSale = true que pertenecen
            // a la organización de esa ubicación.
            var location = await _context.Locations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.Id == locationId);

            if (location == null)
                return Enumerable.Empty<PublicCatalogItemResponse>();

            var businessHours = DeserializeBusinessHours(location.BusinessHoursJson);
            var isOpenNow = CalculateIsOpenNow(businessHours, DateTime.Now);

            // Inventario por ubicación: inventariables con stock > 0 en su fila o en el producto padre (venta fraccionada).
            var productIdsWithPositiveStock = await _context.Inventories
                .IgnoreQueryFilters()
                .Where(i => i.LocationId == locationId && i.CurrentStock > 0)
                .Select(i => i.ProductId)
                .Distinct()
                .ToListAsync();
            var parentIdsWithStock = productIdsWithPositiveStock.ToHashSet();

            var childProductIdsWithParentStock = await _context.Products
                .IgnoreQueryFilters()
                .Where(p => p.OrganizationId == location.OrganizationId
                    && p.IsForSale
                    && !p.IsDeleted
                    && p.Tipo == ProductType.inventariable
                    && p.StockParentProductId != null
                    && p.StockUnitsConsumedPerSaleUnit != null
                    && p.StockUnitsConsumedPerSaleUnit > 0
                    && parentIdsWithStock.Contains(p.StockParentProductId!.Value))
                .Select(p => p.Id)
                .ToListAsync();

            var inventariableIdsAvailable = parentIdsWithStock
                .Union(childProductIdsWithParentStock)
                .ToHashSet();

            var elaboradoOfferProductIds = await _context.ProductLocationOffers
                .IgnoreQueryFilters()
                .Where(o => o.LocationId == locationId && o.OrganizationId == location.OrganizationId)
                .Select(o => o.ProductId)
                .ToListAsync();

            var products = await _context.Products
                .IgnoreQueryFilters()
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductTags)
                .ThenInclude(pt => pt.Tag)
                .Where(p => p.OrganizationId == location.OrganizationId
                    && p.IsForSale
                    && !p.IsDeleted)
                .ToListAsync();

            var promotions = await GetActivePromotionsMapAsync(products.Select(p => p.Id).ToList(), location.OrganizationId);

            if (products.Count == 0)
                return Enumerable.Empty<PublicCatalogItemResponse>();

            var invDecimals = _inventorySettings.RoundingDecimals;
            var inventories = await _context.Inventories
                .IgnoreQueryFilters()
                .Where(i => i.LocationId == locationId)
                .ToDictionaryAsync(i => i.ProductId, i => i.CurrentStock);

            var result = products
                .Where(p =>
                    (p.Tipo == ProductType.inventariable && inventariableIdsAvailable.Contains(p.Id))
                    || (p.Tipo == ProductType.elaborado && elaboradoOfferProductIds.Contains(p.Id)))
                .Select(p =>
                {
                    promotions.TryGetValue(p.Id, out var promo);
                    var effectivePrice = promo != null ? CalculatePromotionalPrice(p.Precio, promo) : p.Precio;
                    decimal stockDisplay = 0;
                    if (p.Tipo == ProductType.inventariable)
                    {
                        if (p.StockParentProductId is int ppid
                            && p.StockUnitsConsumedPerSaleUnit is decimal f
                            && f > 0)
                        {
                            var parentStock = inventories.TryGetValue(ppid, out var ps) ? ps : 0;
                            stockDisplay = ProductStockResolution.GetSaleUnitsFromParentStock(parentStock, f, invDecimals);
                        }
                        else
                            stockDisplay = inventories.TryGetValue(p.Id, out var s) ? s : 0;
                    }

                    return new PublicCatalogItemResponse
                    {
                        Id = p.Id,
                        Code = p.Code,
                        Name = p.Name,
                        Description = p.Description,
                        ImagenUrl = ProductPrimaryImageUrlResolver.Resolve(p),
                        Images = MapCatalogImages(p),
                        Precio = effectivePrice,
                        OriginalPrecio = p.Precio,
                        HasActivePromotion = promo != null,
                        PromotionType = promo?.Type.ToString(),
                        PromotionValue = promo?.Value,
                        PromotionId = promo?.Id,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category?.Name,
                        CategoryColor = p.Category?.Color,
                        Tipo = p.Tipo.ToString(),
                        StockAtLocation = stockDisplay,
                        IsOpenNow = isOpenNow,
                        Tags = p.ProductTags?.Select(pt => pt.Tag).Where(t => t != null).Select(t => new TagDto { Id = t!.Id, Name = t.Name, Slug = t.Slug, Color = t.Color }).ToList() ?? new List<TagDto>(),
                    };
                });

            return result;
        }

        public async Task<PublicCatalogPaginatedResponse> GetCatalogAllAsync(
            int page, int pageSize,
            string? sortBy = null, string? sortDir = null,
            int? tagId = null, decimal? minPrice = null, decimal? maxPrice = null,
            bool? inStock = null, bool? hasPromotion = null)
        {
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 50;
            }

            // 1) Traer todas las ubicaciones públicas (mismas que GetLocationsAsync).
            var locations = await _context.Locations
                .IgnoreQueryFilters()
                .Include(l => l.Organization)
                .OrderBy(l => l.Organization!.Name)
                .ThenBy(l => l.Name)
                .ToListAsync();

            var now = DateTime.Now;

            var locationInfos = locations.Select(l =>
            {
                var bh = DeserializeBusinessHours(l.BusinessHoursJson);
                var isOpen = CalculateIsOpenNow(bh, now);

                return new
                {
                    Location = l,
                    BusinessHours = bh,
                    IsOpenNow = (bool?)isOpen
                };
            }).ToList();

            var locationIds = locationInfos.Select(li => li.Location.Id).ToList();

            // 2) Inventarios: por cada ubicación, qué productos tienen (cada ubicación tiene su propio listado)
            var inventories = await _context.Inventories
                .IgnoreQueryFilters()
                .Where(i => locationIds.Contains(i.LocationId))
                .ToListAsync();

            var inventoryLookup = inventories.ToDictionary(
                i => (i.ProductId, i.LocationId),
                i => i.CurrentStock);

            var offerPairs = await _context.ProductLocationOffers
                .IgnoreQueryFilters()
                .Where(o => locationIds.Contains(o.LocationId))
                .Select(o => new { o.ProductId, o.LocationId })
                .ToListAsync();
            var elaboradoOfferSet = offerPairs.Select(o => (o.ProductId, o.LocationId)).ToHashSet();

            if (locationInfos.Count == 0)
            {
                return new PublicCatalogPaginatedResponse
                {
                    Items = Array.Empty<PublicCatalogItemResponse>(),
                    Page = page,
                    PageSize = pageSize,
                    Total = 0,
                    TotalPages = 0
                };
            }

            var products = await _context.Products
                .IgnoreQueryFilters()
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductTags)
                .ThenInclude(pt => pt.Tag)
                .Where(p => p.IsForSale && !p.IsDeleted)
                .ToDictionaryAsync(p => p.Id, p => p);

            var promotions = await GetActivePromotionsMapByOrganizationAsync(products.Keys.ToList());

            var allItems = new List<PublicCatalogItemResponse>();

            foreach (var li in locationInfos)
            {
                var loc = li.Location;
                var productIdsAtLoc = inventories
                    .Where(i => i.LocationId == loc.Id && i.CurrentStock > 0)
                    .Select(i => i.ProductId)
                    .Distinct()
                    .ToList();

                foreach (var productId in productIdsAtLoc)
                {
                    if (!products.TryGetValue(productId, out var p) || p.OrganizationId != loc.OrganizationId)
                        continue;

                    inventoryLookup.TryGetValue((p.Id, loc.Id), out var stock);
                    Promotion? promo = null;
                    if (promotions.TryGetValue(p.Id, out var promosByOrg) && promosByOrg.TryGetValue(loc.OrganizationId, out var scopedPromo))
                    {
                        promo = scopedPromo;
                    }
                    var effectivePrice = promo != null ? CalculatePromotionalPrice(p.Precio, promo) : p.Precio;

                    allItems.Add(new PublicCatalogItemResponse
                    {
                        Id = p.Id,
                        Code = p.Code,
                        Name = p.Name,
                        Description = p.Description,
                        ImagenUrl = ProductPrimaryImageUrlResolver.Resolve(p),
                        Images = MapCatalogImages(p),
                        Precio = effectivePrice,
                        OriginalPrecio = p.Precio,
                        HasActivePromotion = promo != null,
                        PromotionType = promo?.Type.ToString(),
                        PromotionValue = promo?.Value,
                        PromotionId = promo?.Id,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category?.Name,
                        CategoryColor = p.Category?.Color,
                        Tipo = p.Tipo.ToString(),
                        StockAtLocation = stock,
                        IsOpenNow = li.IsOpenNow,
                        LocationId = loc.Id,
                        LocationName = loc.Name,
                        Tags = p.ProductTags?.Select(pt => pt.Tag).Where(t => t != null).Select(t => new TagDto { Id = t!.Id, Name = t.Name, Slug = t.Slug, Color = t.Color }).ToList() ?? new List<TagDto>(),
                    });
                }

                // Inventariables que solo consumen stock del padre (sin fila de inventario propia en esta tienda).
                var decimalsAll = _inventorySettings.RoundingDecimals;
                var idsFromDirectStock = productIdsAtLoc.ToHashSet();
                foreach (var p in products.Values.Where(x =>
                    x.OrganizationId == loc.OrganizationId
                    && x.Tipo == ProductType.inventariable
                    && x.StockParentProductId.HasValue
                    && x.StockUnitsConsumedPerSaleUnit is > 0
                    && !idsFromDirectStock.Contains(x.Id)))
                {
                    if (!inventoryLookup.TryGetValue((p.StockParentProductId!.Value, loc.Id), out var parentStock) || parentStock <= 0)
                        continue;

                    var stockDisplay = ProductStockResolution.GetSaleUnitsFromParentStock(
                        parentStock,
                        p.StockUnitsConsumedPerSaleUnit!.Value,
                        decimalsAll);

                    Promotion? promoCh = null;
                    if (promotions.TryGetValue(p.Id, out var promosByOrgCh) && promosByOrgCh.TryGetValue(loc.OrganizationId, out var scopedPromoCh))
                    {
                        promoCh = scopedPromoCh;
                    }
                    var effectivePriceCh = promoCh != null ? CalculatePromotionalPrice(p.Precio, promoCh) : p.Precio;

                    allItems.Add(new PublicCatalogItemResponse
                    {
                        Id = p.Id,
                        Code = p.Code,
                        Name = p.Name,
                        Description = p.Description,
                        ImagenUrl = ProductPrimaryImageUrlResolver.Resolve(p),
                        Images = MapCatalogImages(p),
                        Precio = effectivePriceCh,
                        OriginalPrecio = p.Precio,
                        HasActivePromotion = promoCh != null,
                        PromotionType = promoCh?.Type.ToString(),
                        PromotionValue = promoCh?.Value,
                        PromotionId = promoCh?.Id,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category?.Name,
                        CategoryColor = p.Category?.Color,
                        Tipo = p.Tipo.ToString(),
                        StockAtLocation = stockDisplay,
                        IsOpenNow = li.IsOpenNow,
                        LocationId = loc.Id,
                        LocationName = loc.Name,
                        Tags = p.ProductTags?.Select(pt => pt.Tag).Where(t => t != null).Select(t => new TagDto { Id = t!.Id, Name = t.Name, Slug = t.Slug, Color = t.Color }).ToList() ?? new List<TagDto>(),
                    });
                }

                // Elaborados: solo si hay fila en ProductLocationOffers para esta tienda.
                var elaboradoProducts = products.Values
                    .Where(p => p.OrganizationId == loc.OrganizationId
                        && p.Tipo == ProductType.elaborado
                        && !productIdsAtLoc.Contains(p.Id)
                        && elaboradoOfferSet.Contains((p.Id, loc.Id)));

                foreach (var p in elaboradoProducts)
                {
                    Promotion? promo = null;
                    if (promotions.TryGetValue(p.Id, out var promosByOrg) && promosByOrg.TryGetValue(loc.OrganizationId, out var scopedPromo))
                    {
                        promo = scopedPromo;
                    }
                    var effectivePrice = promo != null ? CalculatePromotionalPrice(p.Precio, promo) : p.Precio;

                    allItems.Add(new PublicCatalogItemResponse
                    {
                        Id = p.Id,
                        Code = p.Code,
                        Name = p.Name,
                        Description = p.Description,
                        ImagenUrl = ProductPrimaryImageUrlResolver.Resolve(p),
                        Images = MapCatalogImages(p),
                        Precio = effectivePrice,
                        OriginalPrecio = p.Precio,
                        HasActivePromotion = promo != null,
                        PromotionType = promo?.Type.ToString(),
                        PromotionValue = promo?.Value,
                        PromotionId = promo?.Id,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category?.Name,
                        CategoryColor = p.Category?.Color,
                        Tipo = p.Tipo.ToString(),
                        StockAtLocation = 0,
                        IsOpenNow = li.IsOpenNow,
                        LocationId = loc.Id,
                        LocationName = loc.Name,
                        Tags = p.ProductTags?.Select(pt => pt.Tag).Where(t => t != null).Select(t => new TagDto { Id = t!.Id, Name = t.Name, Slug = t.Slug, Color = t.Color }).ToList() ?? new List<TagDto>(),
                    });
                }
            }

            // Filtrado server-side
            IEnumerable<PublicCatalogItemResponse> filtered = allItems;

            if (tagId.HasValue)
                filtered = filtered.Where(i => i.Tags.Any(t => t.Id == tagId.Value));
            if (minPrice.HasValue)
                filtered = filtered.Where(i => i.Precio >= minPrice.Value);
            if (maxPrice.HasValue)
                filtered = filtered.Where(i => i.Precio <= maxPrice.Value);
            if (inStock == true)
                filtered = filtered.Where(i => i.Tipo == "elaborado" || i.StockAtLocation > 0);
            if (hasPromotion == true)
                filtered = filtered.Where(i => i.HasActivePromotion);

            var filteredList = filtered.ToList();

            // Sorting server-side
            var ascending = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
            filteredList = (sortBy?.ToLowerInvariant()) switch
            {
                "price" => ascending
                    ? filteredList.OrderBy(i => i.Precio).ToList()
                    : filteredList.OrderByDescending(i => i.Precio).ToList(),
                "name" => ascending
                    ? filteredList.OrderBy(i => i.Name).ToList()
                    : filteredList.OrderByDescending(i => i.Name).ToList(),
                "newest" => ascending
                    ? filteredList.OrderBy(i => i.Id).ToList()
                    : filteredList.OrderByDescending(i => i.Id).ToList(),
                "promo" => filteredList.OrderByDescending(i => i.HasActivePromotion).ThenBy(i => i.Name).ToList(),
                _ => filteredList.OrderBy(i => i.Name).ThenBy(i => i.LocationName).ToList(),
            };

            var total = filteredList.Count;
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            var skip = (page - 1) * pageSize;

            var pagedItems = filteredList
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            return new PublicCatalogPaginatedResponse
            {
                Items = pagedItems,
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = totalPages
            };
        }

        private static PublicLocationBusinessHoursDto? DeserializeBusinessHours(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<PublicLocationBusinessHoursDto>(json, options);
            }
            catch
            {
                // Si el JSON es inválido, consideramos que no hay horario configurado.
                return null;
            }
        }

        private static bool CalculateIsOpenNow(PublicLocationBusinessHoursDto? businessHours, DateTime now)
        {
            if (businessHours == null)
            {
                return false;
            }

            PublicLocationDayHoursDto? todayHours = now.DayOfWeek switch
            {
                DayOfWeek.Monday => businessHours.Monday,
                DayOfWeek.Tuesday => businessHours.Tuesday,
                DayOfWeek.Wednesday => businessHours.Wednesday,
                DayOfWeek.Thursday => businessHours.Thursday,
                DayOfWeek.Friday => businessHours.Friday,
                DayOfWeek.Saturday => businessHours.Saturday,
                DayOfWeek.Sunday => businessHours.Sunday,
                _ => null
            };

            if (todayHours == null)
            {
                return false;
            }

            if (!TimeSpan.TryParse(todayHours.Open, out var openTime) ||
                !TimeSpan.TryParse(todayHours.Close, out var closeTime))
            {
                return false;
            }

            var currentTime = now.TimeOfDay;

            // Incluimos el horario de apertura y cierre como válidos (>= open, <= close).
            return currentTime >= openTime && currentTime <= closeTime;
        }

        /// <summary>
        /// Todas las URLs de imagen para el catálogo público: galería ordenada o imagen legada única.
        /// </summary>
        private static List<PublicCatalogImageItem> MapCatalogImages(Product p)
        {
            if (p.ProductImages != null && p.ProductImages.Count > 0)
            {
                return p.ProductImages
                    .Where(pi => !string.IsNullOrWhiteSpace(pi.ImageUrl))
                    .OrderBy(pi => pi.SortOrder)
                    .Select(pi => new PublicCatalogImageItem
                    {
                        ImageUrl = pi.ImageUrl!,
                        SortOrder = pi.SortOrder,
                        IsMain = pi.IsMain
                    })
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(p.ImagenUrl))
            {
                return new List<PublicCatalogImageItem>
                {
                    new PublicCatalogImageItem { ImageUrl = p.ImagenUrl, SortOrder = 0, IsMain = true }
                };
            }

            return new List<PublicCatalogImageItem>();
        }

        public async Task<IEnumerable<TagDto>> GetPublicTagsAsync()
        {
            var tagIds = await _context.Products
                .IgnoreQueryFilters()
                .Where(p => p.IsForSale && !p.IsDeleted)
                .SelectMany(p => p.ProductTags)
                .Select(pt => pt.TagId)
                .Distinct()
                .ToListAsync();

            if (tagIds.Count == 0)
                return Enumerable.Empty<TagDto>();

            var tags = await _context.Tags
                .Where(t => tagIds.Contains(t.Id))
                .OrderBy(t => t.Name)
                .Select(t => new TagDto { Id = t.Id, Name = t.Name, Slug = t.Slug, Color = t.Color })
                .ToListAsync();

            return tags;
        }

        public async Task<PublicOrderResponse?> GetPublicOrderByIdAsync(int id)
        {
            var order = await _context.SaleOrders
                .IgnoreQueryFilters()
                .Include(s => s.Items)
                    .ThenInclude(i => i.Product)
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (order == null)
                return null;

            return new PublicOrderResponse
            {
                Id = order.Id,
                Folio = order.Folio,
                Status = order.Status.ToString(),
                Subtotal = order.Subtotal,
                DiscountAmount = order.DiscountAmount,
                Total = order.Total,
                Notes = order.Notes,
                CreatedAt = order.CreatedAt,
                LocationId = order.LocationId,
                LocationName = order.Location?.Name,
                Items = order.Items.Select(i => new PublicOrderItemResponse
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Discount = i.Discount,
                    LineTotal = i.LineTotal
                }).ToList()
            };
        }

        private async Task<Dictionary<int, Promotion>> GetActivePromotionsMapAsync(List<int> productIds, int? organizationId)
        {
            if (productIds.Count == 0)
                return new Dictionary<int, Promotion>();

            var now = DateTime.UtcNow;
            var query = _context.Promotions
                .IgnoreQueryFilters()
                .Where(p => productIds.Contains(p.ProductId)
                    && p.IsActive
                    && p.MinQuantity <= 1
                    && (!p.StartsAt.HasValue || p.StartsAt.Value <= now)
                    && (!p.EndsAt.HasValue || p.EndsAt.Value >= now));

            if (organizationId.HasValue)
                query = query.Where(p => p.OrganizationId == organizationId.Value);

            var promotions = await query
                .OrderByDescending(p => p.Value)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            return promotions
                .GroupBy(p => p.ProductId)
                .ToDictionary(g => g.Key, g => g.First());
        }

        private async Task<Dictionary<int, Dictionary<int, Promotion>>> GetActivePromotionsMapByOrganizationAsync(List<int> productIds)
        {
            if (productIds.Count == 0)
                return new Dictionary<int, Dictionary<int, Promotion>>();

            var now = DateTime.UtcNow;
            var promotions = await _context.Promotions
                .IgnoreQueryFilters()
                .Where(p => productIds.Contains(p.ProductId)
                    && p.IsActive
                    && p.MinQuantity <= 1
                    && (!p.StartsAt.HasValue || p.StartsAt.Value <= now)
                    && (!p.EndsAt.HasValue || p.EndsAt.Value >= now))
                .OrderByDescending(p => p.Value)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

            return promotions
                .GroupBy(p => p.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(x => x.OrganizationId).ToDictionary(og => og.Key, og => og.First()));
        }

        private static decimal CalculatePromotionalPrice(decimal originalPrice, Promotion promotion)
        {
            if (promotion.Type == PromotionType.percentage)
            {
                return Math.Max(0, originalPrice - (originalPrice * (promotion.Value / 100m)));
            }

            return Math.Max(0, promotion.Value);
        }

        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            var dLat = (lat2 - lat1) * Math.PI / 180.0;
            var dLon = (lon2 - lon1) * Math.PI / 180.0;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}
