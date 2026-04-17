using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using APICore.Services.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IPaymentMethodService
    {
        Task<PaymentMethod> CreateAsync(CreatePaymentMethodRequest request);
        Task UpdateAsync(int id, UpdatePaymentMethodRequest request);
        Task DeleteAsync(int id);
        Task<PaymentMethod> GetAsync(int id);
        Task<PaginatedList<PaymentMethod>> GetAllAsync(int? page, int? perPage);
        Task<IReadOnlyList<PaymentMethod>> GetActiveByLocationIdAsync(int locationId);
    }
}
