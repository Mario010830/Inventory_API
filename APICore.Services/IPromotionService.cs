using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using APICore.Services.Utils;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IPromotionService
    {
        Task<Promotion> CreatePromotion(CreatePromotionRequest request);
        Task UpdatePromotion(int id, UpdatePromotionRequest request);
        Task TogglePromotion(int id, bool isActive);
        Task<Promotion> GetPromotion(int id);
        Task<Promotion?> GetActivePromotionForProduct(int productId, decimal quantity, int organizationId);
        Task<PaginatedList<Promotion>> GetPromotions(int? page, int? perPage, int? productId, bool? activeOnly);
    }
}
