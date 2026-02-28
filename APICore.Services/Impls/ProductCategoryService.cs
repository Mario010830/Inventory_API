using APICore.Common.DTO.Request;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using APICore.Services.Utils;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class ProductCategoryService : IProductCategoryService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<IProductCategoryService> _localizer;

        public ProductCategoryService(IUnitOfWork uow, CoreDbContext context, IStringLocalizer<IProductCategoryService> localizer)
        {
            _uow = uow;
            _context = context;
            _localizer = localizer;
        }

        public async Task<ProductCategory> CreateCategory(CreateProductCategoryRequest request)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var nameExists = await _uow.ProductCategoryRepository.FindAllAsync(c => c.Name == request.Name && c.OrganizationId == orgId);
            if (nameExists != null && nameExists.Count > 0)
            {
                throw new ProductCategoryNameInUseBadRequestException(_localizer);
            }

            var newCategory = new ProductCategory
            {
                OrganizationId = orgId,
                Name = request.Name,
                Description = request.Description,
                Color = request.Color ?? "#6366f1",
                Icon = request.Icon ?? "category",
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };

            await _uow.ProductCategoryRepository.AddAsync(newCategory);
            await _uow.CommitAsync();

            return newCategory;
        }

        public async Task DeleteCategory(int id)
        {
            var category = await _uow.ProductCategoryRepository.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                throw new ProductCategoryNotFoundException(_localizer);
            }

            var productsCount = await _uow.ProductRepository.FindAllAsync(p => p.CategoryId == id);
            if (productsCount != null && productsCount.Count > 0)
            {
                throw new CategoryHasProductsBadRequestException(_localizer);
            }

            _uow.ProductCategoryRepository.Delete(category);
            await _uow.CommitAsync();
        }

        public async Task<PaginatedList<ProductCategory>> GetAllCategories(int? page, int? perPage, string sortOrder = null)
        {
            var categories = _uow.ProductCategoryRepository.GetAll();
            var pageIndex = page ?? 1;
            var perPageIndex = perPage ?? 10;
            return await PaginatedList<ProductCategory>.CreateAsync(categories, pageIndex, perPageIndex);
        }

        public async Task<ProductCategory> GetCategory(int id)
        {
            var category = await _uow.ProductCategoryRepository.FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                throw new ProductCategoryNotFoundException(_localizer);
            }
            return category;
        }

        public async Task UpdateCategory(int id, UpdateProductCategoryRequest request)
        {
            var oldCategory = await _uow.ProductCategoryRepository.FirstOrDefaultAsync(c => c.Id == id);
            if (oldCategory == null)
            {
                throw new ProductCategoryNotFoundException(_localizer);
            }

            if (request.Name != null)
            {
                var orgId = _context.CurrentOrganizationId;
                var nameExists = await _uow.ProductCategoryRepository.FindAllAsync(c => c.Name == request.Name && c.Id != id && c.OrganizationId == orgId);
                if (nameExists != null && nameExists.Count > 0)
                {
                    throw new ProductCategoryNameInUseBadRequestException(_localizer);
                }
            }

            var updatedCategory = new ProductCategory
            {
                Id = oldCategory.Id,
                OrganizationId = oldCategory.OrganizationId,
                CreatedAt = oldCategory.CreatedAt,
                ModifiedAt = DateTime.UtcNow,
                Name = request.Name ?? oldCategory.Name,
                Description = request.Description ?? oldCategory.Description,
                Color = request.Color ?? oldCategory.Color,
                Icon = request.Icon ?? oldCategory.Icon,
            };

            await _uow.ProductCategoryRepository.UpdateAsync(updatedCategory, oldCategory.Id);
            await _uow.CommitAsync();
        }
    }
}
