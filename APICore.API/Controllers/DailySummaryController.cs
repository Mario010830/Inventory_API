using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Authorize]
    [Route("api/daily-summary")]
    public class DailySummaryController : Controller
    {
        private readonly IDailySummaryService _dailySummaryService;
        private readonly IPhysicalInventoryCountService _physicalInventoryCountService;

        public DailySummaryController(
            IDailySummaryService dailySummaryService,
            IPhysicalInventoryCountService physicalInventoryCountService)
        {
            _dailySummaryService = dailySummaryService ?? throw new ArgumentNullException(nameof(dailySummaryService));
            _physicalInventoryCountService = physicalInventoryCountService ?? throw new ArgumentNullException(nameof(physicalInventoryCountService));
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
        public async Task<IActionResult> GetByDate([FromQuery] DateTime date, [FromQuery] int? locationId = null)
        {
            var result = await _dailySummaryService.GetByDateAsync(date, locationId);
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("id")]
        [RequirePermission(PermissionCodes.DailySummaryView)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetById([FromQuery] int id)
        {
            var result = await _dailySummaryService.GetByIdAsync(id);
            if (result == null)
                return NotFound(new ApiResponse(404));
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("history")]
        [RequirePermission(PermissionCodes.DailySummaryView)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetHistory([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] int? locationId = null)
        {
            var result = await _dailySummaryService.GetHistoryAsync(from, to, locationId);
            return Ok(new ApiOkResponse(result));
        }

        [HttpPost("export/csv")]
        [RequirePermission(PermissionCodes.DailySummaryExport)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ExportCsv([FromBody] DailySummaryExportRequestDto request)
        {
            var bytes    = await _dailySummaryService.ExportCsvAsync(request);
            var fileName = request.Id.HasValue
                ? $"cuadre_{request.Date:yyyy-MM-dd}_{request.Id}.csv"
                : $"cuadre_{request.Date:yyyy-MM-dd}.csv";
            return File(bytes, "text/csv", fileName);
        }

        [HttpPost("export/pdf")]
        [RequirePermission(PermissionCodes.DailySummaryExport)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ExportPdf([FromBody] DailySummaryExportRequestDto request)
        {
            var bytes    = await _dailySummaryService.ExportPdfAsync(request);
            var fileName = request.Id.HasValue
                ? $"cuadre_{request.Date:yyyy-MM-dd}_{request.Id}.pdf"
                : $"cuadre_{request.Date:yyyy-MM-dd}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        [HttpPost("{dailySummaryId:int}/physical-count")]
        [RequirePermission(PermissionCodes.DailySummaryCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GeneratePhysicalCount(int dailySummaryId)
        {
            var result = await _physicalInventoryCountService.GenerateExpectedAsync(dailySummaryId, GetCurrentUserId());
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("{dailySummaryId:int}/physical-count/summary")]
        [RequirePermission(PermissionCodes.DailySummaryView)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPhysicalCountSummary(int dailySummaryId)
        {
            var result = await _physicalInventoryCountService.GetSummaryAsync(dailySummaryId);
            return Ok(new ApiOkResponse(result));
        }

        [HttpPut("physical-count/{physicalInventoryCountId:int}/items")]
        [RequirePermission(PermissionCodes.DailySummaryCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> SavePhysicalCountItems(
            int physicalInventoryCountId,
            [FromBody] SavePhysicalInventoryCountItemsRequest request)
        {
            var result = await _physicalInventoryCountService.SaveItemsAsync(physicalInventoryCountId, request);
            return Ok(new ApiOkResponse(result));
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.UserData)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
