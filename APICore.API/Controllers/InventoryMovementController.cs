using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.API.Utils;
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
    [Route("api/inventory-movement")]
    public class InventoryMovementController : Controller
    {
        private readonly IInventoryMovementService _inventoryMovementService;
        private readonly IDashboardStatsService _dashboardStatsService;
        private readonly IMapper _mapper;

        public InventoryMovementController(IInventoryMovementService inventoryMovementService, IDashboardStatsService dashboardStatsService, IMapper mapper)
        {
            _inventoryMovementService = inventoryMovementService ?? throw new ArgumentNullException(nameof(inventoryMovementService));
            _dashboardStatsService = dashboardStatsService ?? throw new ArgumentNullException(nameof(dashboardStatsService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.InventoryMovementCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateMovement([FromBody] CreateInventoryMovementRequest request)
        {
            var result = await _inventoryMovementService.CreateMovement(request, User.GetUserIdFromToken());
            var response = _mapper.Map<InventoryMovementResponse>(result);
            return Created("", new ApiCreatedResponse(response));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.InventoryMovementRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetMovements(int? page, int? perPage, string sortOrder = null)
        {
            var movements = await _inventoryMovementService.GetAllMovements(page, perPage, sortOrder);
            var list = _mapper.Map<IEnumerable<InventoryMovementResponse>>(movements);
            return Ok(new ApiOkPaginatedResponse(list, movements.GetPaginationData));
        }

        [HttpGet("id")]
        [RequirePermission(PermissionCodes.InventoryMovementRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetMovementById(int id)
        {
            var movement = await _inventoryMovementService.GetMovement(id);
            var response = _mapper.Map<InventoryMovementResponse>(movement);
            return Ok(new ApiOkResponse(response));
        }

        [HttpGet("product/{productId}")]
        [RequirePermission(PermissionCodes.InventoryMovementRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetMovementsByProduct(int productId, [FromQuery] int locationId, int? page, int? perPage)
        {
            var movements = await _inventoryMovementService.GetMovementsByProduct(productId, locationId, page, perPage);
            var list = _mapper.Map<IEnumerable<InventoryMovementResponse>>(movements);
            return Ok(new ApiOkPaginatedResponse(list, movements.GetPaginationData));
        }

        [HttpGet("stats")]
        [RequirePermission(PermissionCodes.InventoryMovementRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStats([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] bool today = false)
        {
            var result = await _dashboardStatsService.GetMovementStatsAsync(from, to, today);
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("flow-with-cumulative")]
        [RequirePermission(PermissionCodes.InventoryMovementRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetFlowWithCumulative([FromQuery] int days = 7)
        {
            if (days < 1 || days > 90) days = 7;
            var result = await _dashboardStatsService.GetMovementFlowWithCumulativeAsync(days);
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("distribution-by-type")]
        [RequirePermission(PermissionCodes.InventoryMovementRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDistributionByType()
        {
            var result = await _dashboardStatsService.GetMovementDistributionByTypeAsync();
            return Ok(new ApiOkResponse(result));
        }
    }
}
