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
        [RequirePermission(PermissionCodes.ProductRead, PermissionCodes.ProductCreate, PermissionCodes.ProductUpdate, PermissionCodes.InventoryMovementCreate, PermissionCodes.SaleCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetProducts(int? page, int? perPage, string sortOrder = null, bool? onlyForSale = null)
        {
            var products = await _productService.GetAllProducts(page, perPage, sortOrder, onlyForSale);
            var list = _mapper.Map<IEnumerable<ProductResponse>>(products).ToList();
            if (list.Count > 0)
            {
                var stockByProduct = await _productService.GetTotalStockByProductIdsAsync(list.Select(p => p.Id));
                foreach (var item in list)
                    item.TotalStock = stockByProduct.TryGetValue(item.Id, out var total) ? total : 0;
            }
            return Ok(new ApiOkPaginatedResponse(list, products.GetPaginationData));
        }

        /// <summary>
        /// Catálogo de ventas: devuelve solo los productos con IsForSale = true,
        /// con su stock actual. Accesible con permiso SaleRead (no requiere ProductRead).
        /// Este es el endpoint que usa el punto de venta en el front.
        /// </summary>
        [HttpGet("catalog")]
        [RequirePermission(PermissionCodes.SaleRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCatalog(int? page, int? perPage)
        {
            var products = await _productService.GetCatalog(page, perPage);
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
        [RequirePermission(PermissionCodes.ProductRead, PermissionCodes.ProductCreate, PermissionCodes.ProductUpdate, PermissionCodes.InventoryMovementCreate, PermissionCodes.SaleCreate)]
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

        [AllowAnonymous]
        [HttpGet("{id}/images")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetProductImages(int id)
        {
            var images = await _productService.GetProductImagesOrderedAsync(id, ignoreQueryFilters: true);
            var list = _mapper.Map<List<ProductImageResponse>>(images);
            return Ok(new ApiOkResponse(list));
        }

        /// <summary>
        /// Sube una o varias imágenes para el producto. Si no hay imagen principal, la primera subida queda como principal.
        /// </summary>
        [HttpPost("{id}/images")]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [RequirePermission(PermissionCodes.ProductUpdate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UploadProductImages(int id, [FromForm] List<IFormFile> files)
        {
            var fileList = files?.Where(f => f != null && f.Length > 0).ToList() ?? new List<IFormFile>();
            if (fileList.Count == 0 && Request.HasFormContentType)
                fileList = Request.Form.Files.Where(f => f.Length > 0).ToList();
            if (fileList.Count == 0)
                return BadRequest(new ApiBadRequestResponse("Debe enviar al menos un archivo de imagen."));

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            foreach (var file in fileList)
            {
                if (!allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                    return BadRequest(new ApiBadRequestResponse("Solo se permiten imágenes: JPEG, PNG, GIF, WebP."));
                if (file.Length > 5 * 1024 * 1024)
                    return BadRequest(new ApiBadRequestResponse("El archivo no debe superar 5 MB."));
            }

            var uploads = new List<(System.IO.Stream Stream, string FileName, string ContentType)>();
            try
            {
                foreach (var file in fileList)
                {
                    uploads.Add((file.OpenReadStream(), file.FileName, file.ContentType));
                }

                var images = await _productService.UploadProductImagesAsync(id, uploads);
                var response = _mapper.Map<List<ProductImageResponse>>(images);
                return Ok(new ApiOkResponse(response));
            }
            finally
            {
                foreach (var (stream, _, _) in uploads)
                    await stream.DisposeAsync();
            }
        }

        /// <summary>
        /// Marca una imagen como principal y la coloca en SortOrder 0.
        /// </summary>
        [HttpPut("{id}/images/{imageId}/main")]
        [RequirePermission(PermissionCodes.ProductUpdate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> SetMainProductImage(int id, int imageId)
        {
            await _productService.SetProductImageAsMainAsync(id, imageId);
            return NoContent();
        }

        /// <summary>
        /// Actualiza el orden de todas las imágenes. La primera posición tras ordenar por sortOrder queda como principal.
        /// </summary>
        [HttpPut("{id}/images/reorder")]
        [RequirePermission(PermissionCodes.ProductUpdate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> ReorderProductImages(int id, [FromBody] List<ReorderProductImageItemRequest> items)
        {
            await _productService.ReorderProductImagesAsync(id, items);
            return NoContent();
        }

        /// <summary>
        /// Elimina una imagen del producto. Si era la principal, la siguiente por orden pasa a ser principal.
        /// </summary>
        [HttpDelete("{id}/images/{imageId}")]
        [RequirePermission(PermissionCodes.ProductUpdate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteProductImage(int id, int imageId)
        {
            await _productService.DeleteProductImageAsync(id, imageId);
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
