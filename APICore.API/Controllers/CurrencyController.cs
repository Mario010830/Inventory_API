using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Authorize]
    [Route("api/currency")]
    public class CurrencyController : Controller
    {
        private readonly ICurrencyService _currencyService;

        public CurrencyController(ICurrencyService currencyService)
        {
            _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.CurrencyRead, PermissionCodes.CurrencyCreate, PermissionCodes.CurrencyUpdate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAll()
        {
            var items = await _currencyService.GetAllAsync();
            return Ok(new ApiOkResponse(items));
        }

        [HttpGet("{id:int}")]
        [RequirePermission(PermissionCodes.CurrencyRead, PermissionCodes.CurrencyCreate, PermissionCodes.CurrencyUpdate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _currencyService.GetByIdAsync(id);
            return Ok(new ApiOkResponse(item));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.CurrencyCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateCurrencyRequest request)
        {
            var item = await _currencyService.CreateAsync(request);
            return Created("", new ApiCreatedResponse(item));
        }

        [HttpPut("{id:int}")]
        [RequirePermission(PermissionCodes.CurrencyUpdate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCurrencyRequest request)
        {
            var item = await _currencyService.UpdateAsync(id, request);
            return Ok(new ApiOkResponse(item));
        }

        [HttpDelete("{id:int}")]
        [RequirePermission(PermissionCodes.CurrencyDelete)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _currencyService.DeleteAsync(id);
            return NoContent();
        }

        /// <summary>Establece la moneda predeterminada para mostrar en la aplicación (se guarda en configuración).</summary>
        [HttpPut("default")]
        [RequirePermission(PermissionCodes.CurrencyUpdate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> SetDefault([FromBody] SetDefaultCurrencyRequest request)
        {
            var item = await _currencyService.SetDefaultDisplayCurrencyAsync(request);
            return Ok(new ApiOkResponse(item));
        }
    }
}
