using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using APICore.Services.Utils;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IProductCategoryService
    {
        Task<ProductCategory> CreateCategory(CreateProductCategoryRequest request);
        Task DeleteCategory(int id);
        Task UpdateCategory(int id, UpdateProductCategoryRequest request);
        Task<ProductCategory> GetCategory(int id);
        Task<PaginatedList<ProductCategory>> GetAllCategories(int? page, int? perPage, string sortOrder = null);
    }
}
