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
    [Route("api/cash-outflow")]
    public class CashOutflowController : Controller
    {
        private readonly ICashOutflowService _cashOutflowService;

        public CashOutflowController(ICashOutflowService cashOutflowService)
        {
            _cashOutflowService = cashOutflowService ?? throw new ArgumentNullException(nameof(cashOutflowService));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.DailySummaryCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> Create([FromBody] CreateCashOutflowRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _cashOutflowService.CreateAsync(request, userId);
            return Created("", new ApiCreatedResponse(result));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.DailySummaryView)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetByDate([FromQuery] DateTime date, [FromQuery] int? locationId = null)
        {
            var list = await _cashOutflowService.GetByDateAsync(date, locationId);
            return Ok(new ApiOkResponse(list));
        }

        [HttpDelete]
        [RequirePermission(PermissionCodes.DailySummaryCreate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> Delete([FromQuery] int id)
        {
            await _cashOutflowService.DeleteAsync(id);
            return NoContent();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.UserData)?.Value;
            return int.TryParse(claim, out var uid) ? uid : 0;
        }
    }
}
