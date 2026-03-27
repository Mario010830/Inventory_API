using APICore.Common.DTO.Request;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Data.UoW;
using APICore.Services;
using APICore.Services.Exceptions;
using APICore.Services.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class ProductService : IProductService
    {
        private const int MaxProductImagesPerProduct = 8;

        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStorageService _storageService;
        private readonly IStringLocalizer<IProductService> _localizer;
        private readonly ISubscriptionQuotaService _subscriptionQuotaService;

        public ProductService(IUnitOfWork uow, CoreDbContext context, IStorageService storageService, IStringLocalizer<IProductService> localizer, ISubscriptionQuotaService subscriptionQuotaService)
        {
            _uow = uow;
            _context = context;
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _localizer = localizer;
            _subscriptionQuotaService = subscriptionQuotaService ?? throw new ArgumentNullException(nameof(subscriptionQuotaService));
        }

        public async Task<Product> CreateProduct(CreateProductRequest request)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            await _subscriptionQuotaService.EnsureCanAddProductAsync(orgId);

            int? categoryId = null;
            if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
            {
                var categoryExists = await _uow.ProductCategoryRepository.FirstOrDefaultAsync(c => c.Id == request.CategoryId.Value);
                if (categoryExists == null)
                {
                    throw new ProductCategoryNotFoundException(_localizer);
                }

                categoryId = request.CategoryId.Value;
            }

            var codeExists = await _uow.ProductRepository.FindAllAsync(p => p.Code == request.Code && p.OrganizationId == orgId);
            if (codeExists != null && codeExists.Count > 0)
            {
                throw new ProductCodeInUseBadRequestException(_localizer);
            }

            var tipo = ProductType.inventariable;
            if (!string.IsNullOrWhiteSpace(request.Tipo) && Enum.TryParse<ProductType>(request.Tipo, true, out var parsedTipo))
                tipo = parsedTipo;

            var newProduct = new Product
            {
                OrganizationId = orgId,
                Code = request.Code,
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                CategoryId = categoryId,
                Precio = request.Precio,
                Costo = request.Costo,
                ImagenUrl = request.ImagenUrl ?? string.Empty,
                IsAvailable = request.IsAvailable,
                IsForSale = request.IsForSale,
                Tipo = tipo,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };

            await _uow.ProductRepository.AddAsync(newProduct);
            await _uow.CommitAsync();

            if (request.TagIds != null && request.TagIds.Count > 0)
            {
                var tagIds = request.TagIds.Distinct().ToList();
                var existingTags = await _context.Tags.Where(t => tagIds.Contains(t.Id)).Select(t => t.Id).ToListAsync();
                foreach (var tagId in tagIds)
                {
                    if (existingTags.Contains(tagId))
                        _context.ProductTags.Add(new ProductTag { ProductId = newProduct.Id, TagId = tagId });
                }
            }

            await ReplaceProductLocationOffersForElaboradoAsync(newProduct.Id, orgId, tipo, request.OfferLocationIds, isUpdate: false);
            await _uow.CommitAsync();

            return newProduct;
        }

        public async Task DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                throw new ProductNotFoundException(_localizer);
            }

            foreach (var pi in product.ProductImages)
            {
                try
                {
                    await _storageService.DeleteProductImageAsync(pi.ImageUrl);
                }
                catch
                {
                    // Si falla el borrado en almacenamiento, continuamos con el borrado en BD
                }
            }

            // Eliminar imagen única legada si existe
            if (!string.IsNullOrWhiteSpace(product.ImagenUrl))
            {
                try
                {
                    await _storageService.DeleteProductImageAsync(product.ImagenUrl);
                }
                catch
                {
                    // Si falla el borrado en S3, continuamos con el borrado del producto en BD
                }
            }

            _uow.ProductRepository.Delete(product);
            await _uow.CommitAsync();
        }

        public async Task<PaginatedList<Product>> GetAllProducts(int? page, int? perPage, string sortOrder = null, bool? onlyForSale = null)
        {
            IQueryable<Product> query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.LocationOffers)
                .Include(p => p.ProductTags)
                .ThenInclude(pt => pt.Tag);
            if (onlyForSale == true)
                query = query.Where(p => p.IsForSale);
            var pageIndex = page ?? 1;
            var perPageIndex = perPage ?? 10;
            return await PaginatedList<Product>.CreateAsync(query.OrderBy(p => p.Code), pageIndex, perPageIndex);
        }

        public async Task<PaginatedList<Product>> GetCatalog(int? page, int? perPage)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.LocationOffers)
                .Include(p => p.ProductTags)
                .ThenInclude(pt => pt.Tag)
                .Where(p => p.IsForSale);
            return await PaginatedList<Product>.CreateAsync(query, page ?? 1, perPage ?? 50);
        }

        public async Task<Product> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.LocationOffers)
                .Include(p => p.ProductTags)
                .ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                throw new ProductNotFoundException(_localizer);
            }
            return product;
        }

        public async Task UpdateProduct(int id, UpdateProductRequest request)
        {
            var oldProduct = await _uow.ProductRepository.FirstOrDefaultAsync(p => p.Id == id);
            if (oldProduct == null)
            {
                throw new ProductNotFoundException(_localizer);
            }

            int? resolvedCategoryId = oldProduct.CategoryId;
            if (request.CategoryId != null)
            {
                if (request.CategoryId.Value <= 0)
                {
                    resolvedCategoryId = null;
                }
                else
                {
                    var categoryExists = await _uow.ProductCategoryRepository.FirstOrDefaultAsync(c => c.Id == request.CategoryId.Value);
                    if (categoryExists == null)
                    {
                        throw new ProductCategoryNotFoundException(_localizer);
                    }

                    resolvedCategoryId = request.CategoryId.Value;
                }
            }

            if (request.Code != null)
            {
                var orgId = _context.CurrentOrganizationId;
                var codeExists = await _uow.ProductRepository.FindAllAsync(p => p.Code == request.Code && p.Id != id && p.OrganizationId == orgId);
                if (codeExists != null && codeExists.Count > 0)
                {
                    throw new ProductCodeInUseBadRequestException(_localizer);
                }
            }

            var tipo = oldProduct.Tipo;
            if (request.Tipo != null && Enum.TryParse<ProductType>(request.Tipo, true, out var parsedTipo))
                tipo = parsedTipo;

            var updatedProduct = new Product
            {
                Id = oldProduct.Id,
                OrganizationId = oldProduct.OrganizationId,
                CreatedAt = oldProduct.CreatedAt,
                ModifiedAt = DateTime.UtcNow,
                Code = request.Code ?? oldProduct.Code,
                Name = request.Name ?? oldProduct.Name,
                Description = request.Description ?? oldProduct.Description,
                CategoryId = resolvedCategoryId,
                Precio = request.Precio ?? oldProduct.Precio,
                Costo = request.Costo ?? oldProduct.Costo,
                ImagenUrl = request.ImagenUrl ?? oldProduct.ImagenUrl,
                IsAvailable = request.IsAvailable ?? oldProduct.IsAvailable,
                IsForSale = request.IsForSale ?? oldProduct.IsForSale,
                Tipo = tipo,
            };

            await _uow.ProductRepository.UpdateAsync(updatedProduct, oldProduct.Id);

            if (request.TagIds != null)
            {
                var existing = await _context.ProductTags.Where(pt => pt.ProductId == id).ToListAsync();
                _context.ProductTags.RemoveRange(existing);
                var tagIds = request.TagIds.Distinct().ToList();
                if (tagIds.Count > 0)
                {
                    var validTagIds = await _context.Tags.Where(t => tagIds.Contains(t.Id)).Select(t => t.Id).ToListAsync();
                    foreach (var tagId in validTagIds)
                        _context.ProductTags.Add(new ProductTag { ProductId = id, TagId = tagId });
                }
            }

            await ReplaceProductLocationOffersForElaboradoAsync(id, oldProduct.OrganizationId, tipo, request.OfferLocationIds, isUpdate: true);

            await _uow.CommitAsync();
        }

        private async Task ReplaceProductLocationOffersForElaboradoAsync(int productId, int orgId, ProductType tipo, List<int>? offerLocationIds, bool isUpdate)
        {
            if (tipo != ProductType.elaborado)
            {
                var toRemove = await _context.ProductLocationOffers.Where(o => o.ProductId == productId).ToListAsync();
                if (toRemove.Count > 0)
                    _context.ProductLocationOffers.RemoveRange(toRemove);
                return;
            }

            if (isUpdate && offerLocationIds == null)
                return;

            var ids = (offerLocationIds ?? new List<int>()).Distinct().ToList();
            var existingOffers = await _context.ProductLocationOffers.Where(o => o.ProductId == productId).ToListAsync();
            _context.ProductLocationOffers.RemoveRange(existingOffers);

            if (ids.Count == 0)
                return;

            var validCount = await _context.Locations.CountAsync(l => ids.Contains(l.Id) && l.OrganizationId == orgId);
            if (validCount != ids.Count)
                throw new LocationNotInOrganizationBadRequestException(_localizer);

            var now = DateTime.UtcNow;
            foreach (var locId in ids)
            {
                _context.ProductLocationOffers.Add(new ProductLocationOffer
                {
                    ProductId = productId,
                    LocationId = locId,
                    OrganizationId = orgId,
                    CreatedAt = now,
                    ModifiedAt = now,
                });
            }
        }

        public async Task<decimal> GetTotalStockForProductAsync(int productId)
        {
            var inventories = await _uow.InventoryRepository
                .FindBy(i => i.ProductId == productId)
                .ToListAsync();
            return inventories.Sum(i => i.CurrentStock);
        }

        public async Task<Dictionary<int, decimal>> GetTotalStockByProductIdsAsync(IEnumerable<int> productIds)
        {
            var ids = productIds?.ToList() ?? new List<int>();
            if (ids.Count == 0)
                return new Dictionary<int, decimal>();

            var inventories = await _uow.InventoryRepository
                .FindBy(i => ids.Contains(i.ProductId))
                .ToListAsync();
            return inventories
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.CurrentStock));
        }

        public async Task<IReadOnlyList<ProductImage>> GetProductImagesOrderedAsync(int productId, bool ignoreQueryFilters = false)
        {
            IQueryable<Product> query = _context.Products.Include(p => p.ProductImages);
            if (ignoreQueryFilters)
                query = query.IgnoreQueryFilters();

            var product = await query.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
                throw new ProductNotFoundException(_localizer);

            return product.ProductImages
                .OrderBy(pi => pi.SortOrder)
                .ToList();
        }

        public async Task<IReadOnlyList<ProductImage>> UploadProductImagesAsync(int productId, IReadOnlyList<(Stream Stream, string FileName, string ContentType)> files)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
                throw new ProductNotFoundException(_localizer);

            var existing = product.ProductImages.ToList();
            if (existing.Count + files.Count > MaxProductImagesPerProduct)
                throw new MaxProductImagesBadRequestException(_localizer);

            var hasMain = existing.Any(pi => pi.IsMain);
            var currentMaxSort = existing.Count > 0 ? existing.Max(pi => pi.SortOrder) : -1;
            var tracked = existing.ToList();

            for (var i = 0; i < files.Count; i++)
            {
                var (stream, fileName, contentType) = files[i];
                var url = await _storageService.UploadProductImageAsync(stream, fileName, contentType);

                if (!hasMain && i == 0)
                {
                    foreach (var pi in tracked)
                        pi.SortOrder += 1;

                    var newMain = new ProductImage
                    {
                        ProductId = productId,
                        ImageUrl = url,
                        SortOrder = 0,
                        IsMain = true,
                    };
                    _context.ProductImages.Add(newMain);
                    tracked.Add(newMain);
                    hasMain = true;
                    currentMaxSort = tracked.Count > 0 ? tracked.Max(pi => pi.SortOrder) : 0;
                    continue;
                }

                currentMaxSort++;
                var img = new ProductImage
                {
                    ProductId = productId,
                    ImageUrl = url,
                    SortOrder = currentMaxSort,
                    IsMain = false,
                };
                _context.ProductImages.Add(img);
                tracked.Add(img);
            }

            await _uow.CommitAsync();

            return await _context.ProductImages
                .Where(pi => pi.ProductId == productId)
                .OrderBy(pi => pi.SortOrder)
                .ToListAsync();
        }

        public async Task SetProductImageAsMainAsync(int productId, int imageId)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
                throw new ProductNotFoundException(_localizer);

            var images = product.ProductImages.ToList();
            var target = images.FirstOrDefault(pi => pi.Id == imageId);
            if (target == null)
                throw new ProductImageNotFoundException(_localizer);

            foreach (var pi in images)
                pi.IsMain = false;
            target.IsMain = true;

            var others = images.Where(pi => pi.Id != target.Id).OrderBy(pi => pi.SortOrder).ToList();
            var newOrder = new List<ProductImage> { target }.Concat(others).ToList();
            for (var i = 0; i < newOrder.Count; i++)
                newOrder[i].SortOrder = i;

            await _uow.CommitAsync();
        }

        public async Task ReorderProductImagesAsync(int productId, IReadOnlyList<ReorderProductImageItemRequest> items)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            if (items == null || items.Count == 0)
                throw new ProductImagesReorderInvalidBadRequestException(_localizer);

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
                throw new ProductNotFoundException(_localizer);

            var images = product.ProductImages.ToList();
            if (images.Count != items.Count)
                throw new ProductImagesReorderInvalidBadRequestException(_localizer);

            var imageIds = images.Select(pi => pi.Id).ToHashSet();
            if (items.Any(it => !imageIds.Contains(it.ImageId)) || items.Select(it => it.ImageId).Distinct().Count() != items.Count)
                throw new ProductImagesReorderInvalidBadRequestException(_localizer);

            var ordered = items
                .OrderBy(it => it.SortOrder)
                .Select(it => images.First(pi => pi.Id == it.ImageId))
                .ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i].SortOrder = i;
                ordered[i].IsMain = i == 0;
            }

            await _uow.CommitAsync();
        }

        public async Task DeleteProductImageAsync(int productId, int imageId)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
                throw new ProductNotFoundException(_localizer);

            var images = product.ProductImages.ToList();
            var img = images.FirstOrDefault(pi => pi.Id == imageId);
            if (img == null)
                throw new ProductImageNotFoundException(_localizer);

            var wasMain = img.IsMain;

            try
            {
                await _storageService.DeleteProductImageAsync(img.ImageUrl);
            }
            catch
            {
                // Continuamos eliminando el registro aunque falle el almacenamiento
            }

            _context.ProductImages.Remove(img);

            var remaining = images.Where(pi => pi.Id != imageId).ToList();
            if (remaining.Count == 0)
            {
                await _uow.CommitAsync();
                return;
            }

            List<ProductImage> ordered;
            if (wasMain)
                ordered = remaining.OrderBy(pi => pi.SortOrder).ToList();
            else
            {
                var mainPi = remaining.FirstOrDefault(pi => pi.IsMain);
                if (mainPi != null)
                {
                    var others = remaining.Where(pi => pi.Id != mainPi.Id).OrderBy(pi => pi.SortOrder).ToList();
                    ordered = new List<ProductImage> { mainPi }.Concat(others).ToList();
                }
                else
                    ordered = remaining.OrderBy(pi => pi.SortOrder).ToList();
            }

            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i].SortOrder = i;
                ordered[i].IsMain = i == 0;
            }

            await _uow.CommitAsync();
        }
    }
}
