using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IBusinessCategoryService
    {
        Task<IEnumerable<BusinessCategoryResponse>> GetActiveAsync();
        Task<BusinessCategoryResponse> GetByIdAsync(int id);
        Task<BusinessCategoryResponse> CreateAsync(CreateBusinessCategoryRequest request);
        Task<BusinessCategoryResponse> UpdateAsync(int id, UpdateBusinessCategoryRequest request);
        Task DeleteAsync(int id);
    }
}
