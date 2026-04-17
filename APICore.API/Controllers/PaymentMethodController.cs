using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Authorize]
    [Route("api/payment-method")]
    public class PaymentMethodController : Controller
    {
        private readonly IPaymentMethodService _paymentMethodService;
        private readonly IMapper _mapper;

        public PaymentMethodController(IPaymentMethodService paymentMethodService, IMapper mapper)
        {
            _paymentMethodService = paymentMethodService ?? throw new ArgumentNullException(nameof(paymentMethodService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>Métodos de pago activos de la organización de la ubicación (p. ej. checkout público).</summary>
        [HttpGet("by-location")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetActiveByLocation([FromQuery] int locationId)
        {
            if (locationId <= 0)
                return BadRequest(new ApiResponse(400, "locationId inválido."));

            var list = await _paymentMethodService.GetActiveByLocationIdAsync(locationId);
            var response = _mapper.Map<IEnumerable<PaymentMethodResponse>>(list);
            return Ok(new ApiOkResponse(response));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.PaymentMethodCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> Create([FromBody] CreatePaymentMethodRequest request)
        {
            var entity = await _paymentMethodService.CreateAsync(request);
            var response = _mapper.Map<PaymentMethodResponse>(entity);
            return Created("", new ApiCreatedResponse(response));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.PaymentMethodRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAll(int? page, int? perPage)
        {
            var list = await _paymentMethodService.GetAllAsync(page, perPage);
            var mapped = _mapper.Map<IEnumerable<PaymentMethodResponse>>(list);
            return Ok(new ApiOkPaginatedResponse(mapped, list.GetPaginationData));
        }

        [HttpGet("id")]
        [RequirePermission(PermissionCodes.PaymentMethodRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var entity = await _paymentMethodService.GetAsync(id);
            var response = _mapper.Map<PaymentMethodResponse>(entity);
            return Ok(new ApiOkResponse(response));
        }

        [HttpPut]
        [RequirePermission(PermissionCodes.PaymentMethodUpdate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePaymentMethodRequest request)
        {
            await _paymentMethodService.UpdateAsync(id, request);
            return NoContent();
        }

        [HttpDelete]
        [RequirePermission(PermissionCodes.PaymentMethodDelete)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _paymentMethodService.DeleteAsync(id);
            return NoContent();
        }
    }
}
