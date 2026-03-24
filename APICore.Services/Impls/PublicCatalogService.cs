#nullable enable
using APICore.Common.DTO.Response;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
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

        public PublicCatalogService(CoreDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PublicLocationResponse>> GetLocationsAsync()
        {
            // IgnoreQueryFilters porque el usuario no está autenticado y los filtros
            // globales de multitenancy devolverían vacío (CurrentOrganizationId = -1).
            var locations = await _context.Locations
                .IgnoreQueryFilters()
                .Include(l => l.Organization)
                .Include(l => l.BusinessCategory)
                .OrderBy(l => l.Organization!.Name)
                .ThenBy(l => l.Name)
                .ToListAsync();

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
                    IsOpenNow = CalculateIsOpenNow(businessHours, now),
                    BusinessCategoryId = l.BusinessCategoryId,
                    BusinessCategory = l.BusinessCategory != null
                        ? new BusinessCategorySummaryDto { Name = l.BusinessCategory.Name, Icon = l.BusinessCategory.Icon }
                        : null,
                };
            }).ToList();

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

            // Inventario por ubicación: se usa para inventariables y para exponer stock.
            var productIdsAtLocation = await _context.Inventories
                .IgnoreQueryFilters()
                .Where(i => i.LocationId == locationId)
                .Select(i => i.ProductId)
                .Distinct()
                .ToListAsync();

            var products = await _context.Products
                .IgnoreQueryFilters()
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductTags)
                .ThenInclude(pt => pt.Tag)
                .Where(p => p.OrganizationId == location.OrganizationId
                    && p.IsForSale)
                .ToListAsync();

            if (products.Count == 0)
                return Enumerable.Empty<PublicCatalogItemResponse>();

            var inventories = await _context.Inventories
                .IgnoreQueryFilters()
                .Where(i => i.LocationId == locationId && productIdsAtLocation.Contains(i.ProductId))
                .ToDictionaryAsync(i => i.ProductId, i => i.CurrentStock);

            var result = products
                .Where(p => p.Tipo == ProductType.elaborado || productIdsAtLocation.Contains(p.Id))
                .Select(p => new PublicCatalogItemResponse
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                ImagenUrl = ProductPrimaryImageUrlResolver.Resolve(p),
                Images = MapCatalogImages(p),
                Precio = p.Precio,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                CategoryColor = p.Category?.Color,
                Tipo = p.Tipo.ToString(),
                StockAtLocation = inventories.TryGetValue(p.Id, out var stock) ? stock : 0,
                IsOpenNow = isOpenNow,
                Tags = p.ProductTags?.Select(pt => pt.Tag).Where(t => t != null).Select(t => new TagDto { Id = t!.Id, Name = t.Name, Slug = t.Slug, Color = t.Color }).ToList() ?? new List<TagDto>(),
            });

            return result;
        }

        public async Task<PublicCatalogPaginatedResponse> GetCatalogAllAsync(int page, int pageSize)
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

            var productIdsInCatalog = inventories.Select(i => i.ProductId).Distinct().ToList();
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
                .Where(p => p.IsForSale)
                .ToDictionaryAsync(p => p.Id, p => p);

            var allItems = new List<PublicCatalogItemResponse>();

            foreach (var li in locationInfos)
            {
                var loc = li.Location;
                var productIdsAtLoc = inventories.Where(i => i.LocationId == loc.Id).Select(i => i.ProductId).Distinct().ToList();

                foreach (var productId in productIdsAtLoc)
                {
                    if (!products.TryGetValue(productId, out var p) || p.OrganizationId != loc.OrganizationId)
                        continue;

                    inventoryLookup.TryGetValue((p.Id, loc.Id), out var stock);

                    allItems.Add(new PublicCatalogItemResponse
                    {
                        Id = p.Id,
                        Code = p.Code,
                        Name = p.Name,
                        Description = p.Description,
                        ImagenUrl = ProductPrimaryImageUrlResolver.Resolve(p),
                        Images = MapCatalogImages(p),
                        Precio = p.Precio,
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

                // Los elaborados se muestran en catálogo público por "en venta",
                // aunque no tengan stock/registro de inventario.
                var elaboradoProducts = products.Values
                    .Where(p => p.OrganizationId == loc.OrganizationId
                        && p.Tipo == ProductType.elaborado
                        && !productIdsAtLoc.Contains(p.Id));

                foreach (var p in elaboradoProducts)
                {
                    allItems.Add(new PublicCatalogItemResponse
                    {
                        Id = p.Id,
                        Code = p.Code,
                        Name = p.Name,
                        Description = p.Description,
                        ImagenUrl = ProductPrimaryImageUrlResolver.Resolve(p),
                        Images = MapCatalogImages(p),
                        Precio = p.Precio,
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

            var total = allItems.Count;
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            var skip = (page - 1) * pageSize;

            var pagedItems = allItems
                .OrderBy(i => i.Name)
                .ThenBy(i => i.LocationName)
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
                .Where(p => p.IsForSale)
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
    }
}
