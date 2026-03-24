using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface ICurrencyService
    {
        Task EnsureBaseCurrencyForOrganizationAsync(int organizationId);
        Task<IEnumerable<CurrencyResponse>> GetAllAsync();
        Task<CurrencyResponse> GetByIdAsync(int id);
        Task<CurrencyResponse> CreateAsync(CreateCurrencyRequest request);
        Task<CurrencyResponse> UpdateAsync(int id, UpdateCurrencyRequest request);
        Task DeleteAsync(int id);
        Task<CurrencyResponse> SetDefaultDisplayCurrencyAsync(SetDefaultCurrencyRequest request);
    }
}
