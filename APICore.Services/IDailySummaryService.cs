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
        Task<IReadOnlyList<DailySummaryResponseDto>> GetByDateAsync(DateTime date, int? locationId = null);
        Task<DailySummaryResponseDto?> GetByIdAsync(int id);
        Task<List<DailySummaryResponseDto>> GetHistoryAsync(DateTime from, DateTime to, int? locationId = null);
        Task<byte[]> ExportCsvAsync(DailySummaryExportRequestDto request);
        Task<byte[]> ExportPdfAsync(DailySummaryExportRequestDto request);
    }
}
