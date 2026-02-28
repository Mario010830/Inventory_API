using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using APICore.Services.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IProductService
    {
        Task<Product> CreateProduct(CreateProductRequest request);
        Task DeleteProduct(int id);
        Task UpdateProduct(int id, UpdateProductRequest request);
        Task<Product> GetProduct(int id);
        Task<PaginatedList<Product>> GetAllProducts(int? page, int? perPage, string sortOrder = null);
        Task<decimal> GetTotalStockForProductAsync(int productId);
        Task<Dictionary<int, decimal>> GetTotalStockByProductIdsAsync(IEnumerable<int> productIds);
    }
}
