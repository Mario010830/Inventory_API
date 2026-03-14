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
    [Route("api/sale-return")]
    public class SaleReturnController : Controller
    {
        private readonly ISaleReturnService _saleReturnService;
        private readonly IMapper _mapper;

        public SaleReturnController(ISaleReturnService saleReturnService, IMapper mapper)
        {
            _saleReturnService = saleReturnService ?? throw new ArgumentNullException(nameof(saleReturnService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.SaleReturnCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateSaleReturn([FromBody] CreateSaleReturnRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _saleReturnService.CreateSaleReturn(request, userId);
            var response = _mapper.Map<SaleReturnResponse>(result);
            return Created("", new ApiCreatedResponse(response));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.SaleRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetSaleReturns(int? page, int? perPage, string? sortOrder)
        {
            var returns = await _saleReturnService.GetAllSaleReturns(page, perPage, sortOrder);
            var list = _mapper.Map<IEnumerable<SaleReturnResponse>>(returns);
            return Ok(new ApiOkPaginatedResponse(list, returns.GetPaginationData));
        }

        [HttpGet("id")]
        [RequirePermission(PermissionCodes.SaleRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetSaleReturnById(int id)
        {
            var result = await _saleReturnService.GetSaleReturn(id);
            var response = _mapper.Map<SaleReturnResponse>(result);
            return Ok(new ApiOkResponse(response));
        }

        [HttpGet("by-sale-order")]
        [RequirePermission(PermissionCodes.SaleRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetReturnsBySaleOrder(int saleOrderId, int? page, int? perPage)
        {
            var returns = await _saleReturnService.GetReturnsBySaleOrder(saleOrderId, page, perPage);
            var list = _mapper.Map<IEnumerable<SaleReturnResponse>>(returns);
            return Ok(new ApiOkPaginatedResponse(list, returns.GetPaginationData));
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.UserData)?.Value;
            return int.TryParse(claim, out var id) ? id : 0;
        }
    }
}
