using APICore.API.BasicResponses;
using APICore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [AllowAnonymous]
    [Route("api/public")]
    public class PublicController : Controller
    {
        private readonly IPublicCatalogService _publicCatalogService;

        public PublicController(IPublicCatalogService publicCatalogService)
        {
            _publicCatalogService = publicCatalogService ?? throw new ArgumentNullException(nameof(publicCatalogService));
        }

        /// <summary>
        /// Lista todos los negocios/ubicaciones disponibles.
        /// El front muestra esta lista para que el usuario elija qué catálogo ver.
        /// No requiere autenticación.
        /// </summary>
        [HttpGet("locations")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLocations(
            string? sortBy = null, string? sortDir = null,
            double? lat = null, double? lng = null, double? radiusKm = null,
            int? categoryId = null)
        {
            var locations = await _publicCatalogService.GetLocationsAsync(sortBy, sortDir, lat, lng, radiusKm, categoryId);
            return Ok(new ApiOkResponse(locations));
        }

        /// <summary>
        /// Lista etiquetas que tienen al menos un producto público asignado. Para filtros en el catálogo.
        /// No requiere autenticación.
        /// </summary>
        [HttpGet("tags")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPublicTags()
        {
            var tags = await _publicCatalogService.GetPublicTagsAsync();
            return Ok(new ApiOkResponse(tags));
        }

        /// <summary>
        /// Catálogo de productos disponibles para venta en una ubicación específica.
        /// Incluye stock actual en esa ubicación, imagen principal (imagenUrl) y lista images con todas las URLs ordenadas.
        /// No expone costos.
        /// No requiere autenticación.
        /// </summary>
        [HttpGet("catalog")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCatalog(
            int? locationId, bool? all, int? page, int? pageSize,
            string? sortBy = null, string? sortDir = null,
            int? tagId = null, decimal? minPrice = null, decimal? maxPrice = null,
            bool? inStock = null, bool? hasPromotion = null)
        {
            // Si viene locationId → flujo actual (sin cambios).
            if (locationId.HasValue)
            {
                var catalog = await _publicCatalogService.GetCatalogByLocationAsync(locationId.Value);
                return Ok(new ApiOkResponse(catalog));
            }

            // Si viene all=true → nuevo flujo paginado con filtros y sorting.
            if (all == true)
            {
                var effectivePage = page.GetValueOrDefault(1);
                var effectivePageSize = pageSize.GetValueOrDefault(50);

                if (effectivePageSize > 100)
                {
                    effectivePageSize = 100;
                }

                var result = await _publicCatalogService.GetCatalogAllAsync(
                    effectivePage, effectivePageSize,
                    sortBy, sortDir, tagId, minPrice, maxPrice, inStock, hasPromotion);

                return Ok(new
                {
                    data = result.Items,
                    pagination = new
                    {
                        page = result.Page,
                        pageSize = result.PageSize,
                        total = result.Total,
                        totalPages = result.TotalPages
                    }
                });
            }

            // Si no viene ni locationId ni all=true → 400 Bad Request.
            return BadRequest(new ApiBadRequestResponse("Se requiere locationId o all=true"));
        }
    }
}
