using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IPlanService
    {
        Task<IReadOnlyList<Plan>> GetActivePlansAsync();
        Task<Plan> GetByIdAsync(int id);
        Task<Plan> CreateAsync(CreateOrUpdatePlanRequest request);
        Task UpdateAsync(int id, CreateOrUpdatePlanRequest request);
        Task DeleteAsync(int id);
    }
}
