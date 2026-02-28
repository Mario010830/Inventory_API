using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Authorize]
    [Route("api/product")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IStorageService _storageService;
        private readonly IDashboardStatsService _dashboardStatsService;
        private readonly IMapper _mapper;

        public ProductController(IProductService productService, IStorageService storageService, IDashboardStatsService dashboardStatsService, IMapper mapper)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _dashboardStatsService = dashboardStatsService ?? throw new ArgumentNullException(nameof(dashboardStatsService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpPost("image")]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [RequirePermission(PermissionCodes.ProductCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UploadProductImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new ApiBadRequestResponse("Debe enviar un archivo de imagen."));

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new ApiBadRequestResponse("Solo se permiten imágenes: JPEG, PNG, GIF, WebP."));

            if (file.Length > 5 * 1024 * 1024) // 5 MB
                return BadRequest(new ApiBadRequestResponse("El archivo no debe superar 5 MB."));

            using var stream = file.OpenReadStream();
            var url = await _storageService.UploadProductImageAsync(stream, file.FileName, file.ContentType);
            return Ok(new ApiOkResponse(new { ImagenUrl = url }));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.ProductCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            var result = await _productService.CreateProduct(request);
            var response = _mapper.Map<ProductResponse>(result);
            return Created("", new ApiCreatedResponse(response));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.ProductRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetProducts(int? page, int? perPage, string sortOrder = null)
        {
            var products = await _productService.GetAllProducts(page, perPage, sortOrder);
            var list = _mapper.Map<IEnumerable<ProductResponse>>(products).ToList();
            if (list.Count > 0)
            {
                var stockByProduct = await _productService.GetTotalStockByProductIdsAsync(list.Select(p => p.Id));
                foreach (var item in list)
                    item.TotalStock = stockByProduct.TryGetValue(item.Id, out var total) ? total : 0;
            }
            return Ok(new ApiOkPaginatedResponse(list, products.GetPaginationData));
        }

        [HttpGet("id")]
        [RequirePermission(PermissionCodes.ProductRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _productService.GetProduct(id);
            var response = _mapper.Map<ProductResponse>(product);
            response.TotalStock = await _productService.GetTotalStockForProductAsync(id);
            return Ok(new ApiOkResponse(response));
        }

        [HttpPut]
        [RequirePermission(PermissionCodes.ProductUpdate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> EditProduct(int id, [FromBody] UpdateProductRequest request)
        {
            await _productService.UpdateProduct(id, request);
            return NoContent();
        }

        [HttpDelete]
        [RequirePermission(PermissionCodes.ProductDelete)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            await _productService.DeleteProduct(id);
            return NoContent();
        }

        [HttpGet("stats")]
        [RequirePermission(PermissionCodes.ProductRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStats([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _dashboardStatsService.GetProductStatsAsync(from, to);
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("performance")]
        [RequirePermission(PermissionCodes.ProductRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPerformance([FromQuery] int days = 7, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            if (days < 1 || days > 90) days = 7;
            var result = await _dashboardStatsService.GetProductPerformanceAsync(days, from, to);
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("stock-by-category")]
        [RequirePermission(PermissionCodes.ProductRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetStockByCategory()
        {
            var result = await _dashboardStatsService.GetStockByCategoryAsync();
            return Ok(new ApiOkResponse(result));
        }
    }
}
