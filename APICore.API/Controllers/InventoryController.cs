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
    [Route("api/inventory")]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly IDashboardStatsService _dashboardStatsService;
        private readonly IMapper _mapper;
        private readonly ICurrentUserContextAccessor _currentUserContextAccessor;

        public InventoryController(IInventoryService inventoryService, IDashboardStatsService dashboardStatsService, IMapper mapper, ICurrentUserContextAccessor currentUserContextAccessor)
        {
            _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
            _dashboardStatsService = dashboardStatsService ?? throw new ArgumentNullException(nameof(dashboardStatsService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _currentUserContextAccessor = currentUserContextAccessor ?? throw new ArgumentNullException(nameof(currentUserContextAccessor));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.InventoryManage)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateInventory([FromBody] CreateInventoryRequest request)
        {
            var result = await _inventoryService.CreateInventory(request);
            var response = _mapper.Map<InventoryResponse>(result);
            return Created("", new ApiCreatedResponse(response));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.InventoryRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetInventories(int? page, int? perPage, string sortOrder = null)
        {
            var inventories = await _inventoryService.GetAllInventories(page, perPage, sortOrder);
            var list = _mapper.Map<IEnumerable<InventoryResponse>>(inventories);
            return Ok(new ApiOkPaginatedResponse(list, inventories.GetPaginationData));
        }

        [HttpGet("by-product")]
        [RequirePermission(PermissionCodes.InventoryRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetStockByProduct(int? locationId = null)
        {
            var locId = locationId ?? _currentUserContextAccessor.GetCurrent()?.LocationId;
            if (!locId.HasValue || locId.Value <= 0)
                return BadRequest(new ApiResponse(400, "Se requiere locationId o un usuario con ubicación asignada."));
            var list = await _inventoryService.GetStockByProductForLocation(locId.Value);
            return Ok(new ApiOkResponse(list));
        }

        [HttpGet("id")]
        [RequirePermission(PermissionCodes.InventoryRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetInventoryById(int id)
        {
            var inventory = await _inventoryService.GetInventory(id);
            var response = _mapper.Map<InventoryResponse>(inventory);
            return Ok(new ApiOkResponse(response));
        }

        [HttpPut]
        [RequirePermission(PermissionCodes.InventoryManage)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> EditInventory(int id, [FromBody] UpdateInventoryRequest request)
        {
            await _inventoryService.UpdateInventory(id, request);
            return NoContent();
        }

        [HttpDelete]
        [RequirePermission(PermissionCodes.InventoryManage)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteInventory(int id)
        {
            await _inventoryService.DeleteInventory(id);
            return NoContent();
        }

        [HttpGet("stats")]
        [RequirePermission(PermissionCodes.InventoryRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStats()
        {
            var result = await _dashboardStatsService.GetInventoryStatsAsync();
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("flow")]
        [RequirePermission(PermissionCodes.InventoryRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetFlow([FromQuery] int days = 7)
        {
            if (days < 1 || days > 90) days = 7;
            var result = await _dashboardStatsService.GetInventoryFlowAsync(days);
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("stock-by-location")]
        [RequirePermission(PermissionCodes.InventoryRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStockByLocation()
        {
            var result = await _dashboardStatsService.GetStockByLocationAsync();
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("category-distribution")]
        [RequirePermission(PermissionCodes.InventoryRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCategoryDistribution()
        {
            var result = await _dashboardStatsService.GetInventoryCategoryDistributionAsync();
            return Ok(new ApiOkResponse(result));
        }
    }
}
