using APICore.Common.DTO.Request;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using APICore.Services.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStorageService _storageService;
        private readonly IStringLocalizer<IProductService> _localizer;

        public ProductService(IUnitOfWork uow, CoreDbContext context, IStorageService storageService, IStringLocalizer<IProductService> localizer)
        {
            _uow = uow;
            _context = context;
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _localizer = localizer;
        }

        public async Task<Product> CreateProduct(CreateProductRequest request)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var categoryExists = await _uow.ProductCategoryRepository.FirstOrDefaultAsync(c => c.Id == request.CategoryId);
            if (categoryExists == null)
            {
                throw new ProductCategoryNotFoundException(_localizer);
            }

            var codeExists = await _uow.ProductRepository.FindAllAsync(p => p.Code == request.Code && p.OrganizationId == orgId);
            if (codeExists != null && codeExists.Count > 0)
            {
                throw new ProductCodeInUseBadRequestException(_localizer);
            }

            var newProduct = new Product
            {
                OrganizationId = orgId,
                Code = request.Code,
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                CategoryId = request.CategoryId,
                Precio = request.Precio,
                Costo = request.Costo,
                ImagenUrl = request.ImagenUrl ?? string.Empty,
                IsAvailable = request.IsAvailable,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };

            await _uow.ProductRepository.AddAsync(newProduct);
            await _uow.CommitAsync();

            return newProduct;
        }

        public async Task DeleteProduct(int id)
        {
            var product = await _uow.ProductRepository.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
            {
                throw new ProductNotFoundException(_localizer);
            }

            // Eliminar imagen de S3 si existe
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

        public async Task<PaginatedList<Product>> GetAllProducts(int? page, int? perPage, string sortOrder = null)
        {
            var products = _uow.ProductRepository.GetAll();
            var pageIndex = page ?? 1;
            var perPageIndex = perPage ?? 10;
            return await PaginatedList<Product>.CreateAsync(products, pageIndex, perPageIndex);
        }

        public async Task<Product> GetProduct(int id)
        {
            var product = await _uow.ProductRepository.FirstOrDefaultAsync(p => p.Id == id);
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

            if (request.CategoryId.HasValue)
            {
                var categoryExists = await _uow.ProductCategoryRepository.FirstOrDefaultAsync(c => c.Id == request.CategoryId.Value);
                if (categoryExists == null)
                {
                    throw new ProductCategoryNotFoundException(_localizer);
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

            var updatedProduct = new Product
            {
                Id = oldProduct.Id,
                OrganizationId = oldProduct.OrganizationId,
                CreatedAt = oldProduct.CreatedAt,
                ModifiedAt = DateTime.UtcNow,
                Code = request.Code ?? oldProduct.Code,
                Name = request.Name ?? oldProduct.Name,
                Description = request.Description ?? oldProduct.Description,
                CategoryId = request.CategoryId ?? oldProduct.CategoryId,
                Precio = request.Precio ?? oldProduct.Precio,
                Costo = request.Costo ?? oldProduct.Costo,
                ImagenUrl = request.ImagenUrl ?? oldProduct.ImagenUrl,
                IsAvailable = request.IsAvailable ?? oldProduct.IsAvailable,
            };

            await _uow.ProductRepository.UpdateAsync(updatedProduct, oldProduct.Id);
            await _uow.CommitAsync();
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
    }
}
