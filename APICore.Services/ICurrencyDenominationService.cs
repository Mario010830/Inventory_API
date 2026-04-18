using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface ICurrencyDenominationService
    {
        Task<IReadOnlyList<CurrencyDenominationResponse>> GetByCurrencyAsync(int currencyId, bool activeOnly = false);

        Task<CurrencyDenominationResponse> CreateAsync(int currencyId, CreateCurrencyDenominationRequest request);

        Task<CurrencyDenominationResponse> UpdateAsync(int currencyId, int id, UpdateCurrencyDenominationRequest request);

        Task DeleteAsync(int currencyId, int id);
    }
}
