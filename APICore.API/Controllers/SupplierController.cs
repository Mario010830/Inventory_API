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
    [Route("api/supplier")]
    public class SupplierController : Controller
    {
        private readonly ISupplierService _supplierService;
        private readonly IDashboardStatsService _dashboardStatsService;
        private readonly IMapper _mapper;

        public SupplierController(ISupplierService supplierService, IDashboardStatsService dashboardStatsService, IMapper mapper)
        {
            _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
            _dashboardStatsService = dashboardStatsService ?? throw new ArgumentNullException(nameof(dashboardStatsService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.SupplierCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest request)
        {
            var result = await _supplierService.CreateSupplier(request);
            var response = _mapper.Map<SupplierResponse>(result);
            return Created("", new ApiCreatedResponse(response));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.SupplierRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetSuppliers(int? page, int? perPage, string sortOrder = null)
        {
            var suppliers = await _supplierService.GetAllSuppliers(page, perPage, sortOrder);
            var list = _mapper.Map<IEnumerable<SupplierResponse>>(suppliers);
            return Ok(new ApiOkPaginatedResponse(list, suppliers.GetPaginationData));
        }

        [HttpGet("id")]
        [RequirePermission(PermissionCodes.SupplierRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetSupplierById(int id)
        {
            var supplier = await _supplierService.GetSupplier(id);
            var response = _mapper.Map<SupplierResponse>(supplier);
            return Ok(new ApiOkResponse(response));
        }

        [HttpPut]
        [RequirePermission(PermissionCodes.SupplierUpdate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> EditSupplier(int id, [FromBody] UpdateSupplierRequest request)
        {
            await _supplierService.UpdateSupplier(id, request);
            return NoContent();
        }

        [HttpDelete]
        [RequirePermission(PermissionCodes.SupplierDelete)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            await _supplierService.DeleteSupplier(id);
            return NoContent();
        }

        [HttpGet("stats")]
        [RequirePermission(PermissionCodes.SupplierRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStats([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _dashboardStatsService.GetSupplierStatsAsync(from, to);
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("delivery-timeline")]
        [RequirePermission(PermissionCodes.SupplierRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDeliveryTimeline([FromQuery] int? days = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var result = await _dashboardStatsService.GetSupplierDeliveryTimelineAsync(days, from, to);
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("category-distribution")]
        [RequirePermission(PermissionCodes.SupplierRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCategoryDistribution()
        {
            var result = await _dashboardStatsService.GetSupplierCategoryDistributionAsync();
            return Ok(new ApiOkResponse(result));
        }
    }
}
