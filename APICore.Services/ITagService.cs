using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface ITagService
    {
        Task<IEnumerable<TagResponse>> GetAllAsync();
        Task<TagResponse> GetByIdAsync(int id);
        Task<TagResponse> CreateAsync(CreateTagRequest request);
        Task<TagResponse> UpdateAsync(int id, UpdateTagRequest request);
        Task DeleteAsync(int id);
    }
}
