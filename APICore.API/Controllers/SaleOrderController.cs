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
using System.Security.Claims;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Authorize]
    [Route("api/sale-order")]
    public class SaleOrderController : Controller
    {
        private readonly ISaleOrderService _saleOrderService;
        private readonly IMapper _mapper;

        public SaleOrderController(ISaleOrderService saleOrderService, IMapper mapper)
        {
            _saleOrderService = saleOrderService ?? throw new ArgumentNullException(nameof(saleOrderService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateSaleOrder([FromBody] CreateSaleOrderRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _saleOrderService.CreateSaleOrder(request, userId);
            var response = _mapper.Map<SaleOrderResponse>(result);
            return Created("", new ApiCreatedResponse(response));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.SaleRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetSaleOrders(int? page, int? perPage, string? status, string? sortOrder)
        {
            var orders = await _saleOrderService.GetAllSaleOrders(page, perPage, status, sortOrder);
            var list = _mapper.Map<IEnumerable<SaleOrderResponse>>(orders);
            return Ok(new ApiOkPaginatedResponse(list, orders.GetPaginationData));
        }

        [HttpGet("id")]
        [RequirePermission(PermissionCodes.SaleRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetSaleOrderById(int id)
        {
            var order = await _saleOrderService.GetSaleOrder(id);
            var response = _mapper.Map<SaleOrderResponse>(order);
            return Ok(new ApiOkResponse(response));
        }

        [HttpPut]
        [RequirePermission(PermissionCodes.SaleUpdate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateSaleOrder(int id, [FromBody] UpdateSaleOrderRequest request)
        {
            await _saleOrderService.UpdateSaleOrder(id, request);
            return NoContent();
        }

        [HttpPost("{id}/confirm")]
        [RequirePermission(PermissionCodes.SaleCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ConfirmSaleOrder(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _saleOrderService.ConfirmSaleOrder(id, userId);
            var response = _mapper.Map<SaleOrderResponse>(result);
            return Ok(new ApiOkResponse(response));
        }

        [HttpPost("{id}/cancel")]
        [RequirePermission(PermissionCodes.SaleCancel)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CancelSaleOrder(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _saleOrderService.CancelSaleOrder(id, userId);
            var response = _mapper.Map<SaleOrderResponse>(result);
            return Ok(new ApiOkResponse(response));
        }

        [HttpGet("stats")]
        [RequirePermission(PermissionCodes.SaleReport)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStats(int? days)
        {
            var stats = await _saleOrderService.GetStats(days);
            return Ok(new ApiOkResponse(stats));
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.UserData)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
