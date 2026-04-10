using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Authorize]
    [Route("api/daily-summary")]
    public class DailySummaryController : Controller
    {
        private readonly IDailySummaryService _dailySummaryService;

        public DailySummaryController(IDailySummaryService dailySummaryService)
        {
            _dailySummaryService = dailySummaryService ?? throw new ArgumentNullException(nameof(dailySummaryService));
        }

        [HttpPost("generate")]
        [RequirePermission(PermissionCodes.DailySummaryCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Generate([FromBody] DailySummaryRequestDto request)
        {
            var result = await _dailySummaryService.GenerateAsync(request);
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.DailySummaryView)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetByDate([FromQuery] DateTime date)
        {
            var result = await _dailySummaryService.GetByDateAsync(date);
            if (result == null)
                return NotFound(new ApiResponse(404));
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("history")]
        [RequirePermission(PermissionCodes.DailySummaryView)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetHistory([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            var result = await _dailySummaryService.GetHistoryAsync(from, to);
            return Ok(new ApiOkResponse(result));
        }

        [HttpPost("export/csv")]
        [RequirePermission(PermissionCodes.DailySummaryExport)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ExportCsv([FromBody] DailySummaryExportRequestDto request)
        {
            var bytes    = await _dailySummaryService.ExportCsvAsync(request.Date);
            var fileName = $"cuadre_{request.Date:yyyy-MM-dd}.csv";
            return File(bytes, "text/csv", fileName);
        }

        [HttpPost("export/pdf")]
        [RequirePermission(PermissionCodes.DailySummaryExport)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ExportPdf([FromBody] DailySummaryExportRequestDto request)
        {
            var bytes    = await _dailySummaryService.ExportPdfAsync(request.Date);
            var fileName = $"cuadre_{request.Date:yyyy-MM-dd}.pdf";
            return File(bytes, "application/pdf", fileName);
        }
    }
}
