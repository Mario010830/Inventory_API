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
    [Route("api/product-category")]
    public class ProductCategoryController : Controller
    {
        private readonly IProductCategoryService _productCategoryService;
        private readonly IDashboardStatsService _dashboardStatsService;
        private readonly IMapper _mapper;

        public ProductCategoryController(IProductCategoryService productCategoryService, IDashboardStatsService dashboardStatsService, IMapper mapper)
        {
            _productCategoryService = productCategoryService ?? throw new ArgumentNullException(nameof(productCategoryService));
            _dashboardStatsService = dashboardStatsService ?? throw new ArgumentNullException(nameof(dashboardStatsService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.ProductCategoryCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateCategory([FromBody] CreateProductCategoryRequest request)
        {
            var result = await _productCategoryService.CreateCategory(request);
            var response = _mapper.Map<ProductCategoryResponse>(result);
            return Created("", new ApiCreatedResponse(response));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.ProductCategoryRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetCategories(int? page, int? perPage, string sortOrder = null)
        {
            var categories = await _productCategoryService.GetAllCategories(page, perPage, sortOrder);
            var list = _mapper.Map<IEnumerable<ProductCategoryResponse>>(categories);
            return Ok(new ApiOkPaginatedResponse(list, categories.GetPaginationData));
        }

        [HttpGet("id")]
        [RequirePermission(PermissionCodes.ProductCategoryRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var category = await _productCategoryService.GetCategory(id);
            var response = _mapper.Map<ProductCategoryResponse>(category);
            return Ok(new ApiOkResponse(response));
        }

        [HttpPut]
        [RequirePermission(PermissionCodes.ProductCategoryUpdate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> EditCategory(int id, [FromBody] UpdateProductCategoryRequest request)
        {
            await _productCategoryService.UpdateCategory(id, request);
            return NoContent();
        }

        [HttpDelete]
        [RequirePermission(PermissionCodes.ProductCategoryDelete)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            await _productCategoryService.DeleteCategory(id);
            return NoContent();
        }

        [HttpGet("stats")]
        [RequirePermission(PermissionCodes.ProductCategoryRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStats()
        {
            var result = await _dashboardStatsService.GetCategoryStatsAsync();
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("item-distribution")]
        [RequirePermission(PermissionCodes.ProductCategoryRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetItemDistribution([FromQuery] string? period = null, [FromQuery] int? days = null)
        {
            var result = await _dashboardStatsService.GetCategoryItemDistributionAsync(period, days);
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("storage-usage")]
        [RequirePermission(PermissionCodes.ProductCategoryRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStorageUsage()
        {
            var result = await _dashboardStatsService.GetCategoryStorageUsageAsync();
            return Ok(new ApiOkResponse(result));
        }
    }
}
