using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IDailySummaryService
    {
        Task<DailySummaryResponseDto> GenerateAsync(DailySummaryRequestDto request);
        Task<DailySummaryResponseDto?> GetByDateAsync(DateTime date, int? locationId = null);
        Task<List<DailySummaryResponseDto>> GetHistoryAsync(DateTime from, DateTime to, int? locationId = null);
        Task<byte[]> ExportCsvAsync(DateTime date);
        Task<byte[]> ExportPdfAsync(DateTime date);
    }
}
