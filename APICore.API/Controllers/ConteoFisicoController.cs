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
    /// <summary>
    /// <c>conteoFisicoId</c> = <see cref="APICore.Data.Entities.PhysicalInventoryCount"/> Id.
    /// </summary>
    [Authorize]
    [Route("api/conteo-fisico")]
    public class ConteoFisicoController : Controller
    {
        private readonly IPhysicalInventoryCountService _physicalInventoryCountService;

        public ConteoFisicoController(IPhysicalInventoryCountService physicalInventoryCountService)
        {
            _physicalInventoryCountService = physicalInventoryCountService ?? throw new ArgumentNullException(nameof(physicalInventoryCountService));
        }

        [HttpPut("{conteoFisicoId:int}/items")]
        [RequirePermission(PermissionCodes.DailySummaryCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GuardarConteo(int conteoFisicoId, [FromBody] SavePhysicalInventoryCountItemsRequest request)
        {
            var result = await _physicalInventoryCountService.SaveItemsAsync(conteoFisicoId, request);
            return Ok(new ApiOkResponse(result));
        }
    }
}
