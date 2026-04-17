using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface ICashOutflowService
    {
        Task<CashOutflowResponseDto> CreateAsync(CreateCashOutflowRequest request, int userId);
        Task DeleteAsync(int id);
        Task<List<CashOutflowResponseDto>> GetByDateAsync(DateTime date, int? locationId);
    }
}
