using APICore.Common.DTO.Response;
using APICore.Data;
using APICore.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                .OrderBy(l => l.Organization!.Name)
                .ThenBy(l => l.Name)
                .Select(l => new PublicLocationResponse
                {
                    Id = l.Id,
                    Name = l.Name,
                    Description = l.Description,
                    OrganizationId = l.OrganizationId,
                    OrganizationName = l.Organization != null ? l.Organization.Name : string.Empty,
                    WhatsAppContact = l.WhatsAppContact,
                })
                .ToListAsync();

            return locations;
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

            // Productos de la organización marcados para venta
            var products = await _context.Products
                .IgnoreQueryFilters()
                .Include(p => p.Category)
                .Where(p => p.OrganizationId == location.OrganizationId && p.IsForSale)
                .ToListAsync();

            if (products.Count == 0)
                return Enumerable.Empty<PublicCatalogItemResponse>();

            // Stock de cada producto en esta ubicación específica
            var productIds = products.Select(p => p.Id).ToList();
            var inventories = await _context.Inventories
                .IgnoreQueryFilters()
                .Where(i => i.LocationId == locationId && productIds.Contains(i.ProductId))
                .ToDictionaryAsync(i => i.ProductId, i => i.CurrentStock);

            var result = products.Select(p => new PublicCatalogItemResponse
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                ImagenUrl = p.ImagenUrl,
                Precio = p.Precio,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                CategoryColor = p.Category?.Color,
                StockAtLocation = inventories.TryGetValue(p.Id, out var stock) ? stock : 0,
            });

            return result;
        }
    }
}
