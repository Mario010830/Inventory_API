using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IPhysicalInventoryCountService
    {
        Task<PhysicalInventoryCountDetailResponse> GenerateExpectedAsync(int dailySummaryId, int userId);
        Task<PhysicalInventoryCountDetailResponse> SaveItemsAsync(int physicalInventoryCountId, SavePhysicalInventoryCountItemsRequest request);
        Task<PhysicalInventoryCountSummaryResponse> GetSummaryAsync(int dailySummaryId);
    }
}
