using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.Common.Constants;
using APICore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    /// <summary>
    /// Alias de rutas para integración con front: <c>cierreCajaId</c> = <see cref="APICore.Data.Entities.DailySummary"/> Id.
    /// </summary>
    [Authorize]
    [Route("api/cierre-caja")]
    public class CierreCajaController : Controller
    {
        private readonly IPhysicalInventoryCountService _physicalInventoryCountService;

        public CierreCajaController(IPhysicalInventoryCountService physicalInventoryCountService)
        {
            _physicalInventoryCountService = physicalInventoryCountService ?? throw new ArgumentNullException(nameof(physicalInventoryCountService));
        }

        [HttpPost("{cierreCajaId:int}/conteo")]
        [RequirePermission(PermissionCodes.DailySummaryCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GenerarConteoEsperado(int cierreCajaId)
        {
            var result = await _physicalInventoryCountService.GenerateExpectedAsync(cierreCajaId, GetCurrentUserId());
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("{cierreCajaId:int}/conteo/resumen")]
        [RequirePermission(PermissionCodes.DailySummaryView)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ObtenerResumenConteo(int cierreCajaId)
        {
            var result = await _physicalInventoryCountService.GetSummaryAsync(cierreCajaId);
            return Ok(new ApiOkResponse(result));
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.UserData)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
