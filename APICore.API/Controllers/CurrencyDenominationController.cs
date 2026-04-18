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
    [Route("api/currency/{currencyId:int}/denominations")]
    public class CurrencyDenominationController : Controller
    {
        private readonly ICurrencyDenominationService _service;

        public CurrencyDenominationController(ICurrencyDenominationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.CurrencyRead, PermissionCodes.CurrencyCreate, PermissionCodes.CurrencyUpdate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetList(int currencyId, [FromQuery] bool activeOnly = false)
        {
            var list = await _service.GetByCurrencyAsync(currencyId, activeOnly);
            return Ok(new ApiOkResponse(list));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.CurrencyCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> Create(int currencyId, [FromBody] CreateCurrencyDenominationRequest request)
        {
            var item = await _service.CreateAsync(currencyId, request);
            return Created("", new ApiCreatedResponse(item));
        }

        [HttpPut("{id:int}")]
        [RequirePermission(PermissionCodes.CurrencyUpdate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Update(int currencyId, int id, [FromBody] UpdateCurrencyDenominationRequest request)
        {
            var item = await _service.UpdateAsync(currencyId, id, request);
            return Ok(new ApiOkResponse(item));
        }

        [HttpDelete("{id:int}")]
        [RequirePermission(PermissionCodes.CurrencyDelete)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> Delete(int currencyId, int id)
        {
            await _service.DeleteAsync(currencyId, id);
            return NoContent();
        }
    }
}
