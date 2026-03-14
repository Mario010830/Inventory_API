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
        public async Task<IActionResult> GetLocations()
        {
            var locations = await _publicCatalogService.GetLocationsAsync();
            return Ok(new ApiOkResponse(locations));
        }

        /// <summary>
        /// Catálogo de productos disponibles para venta en una ubicación específica.
        /// Incluye stock actual en esa ubicación. No expone costos.
        /// No requiere autenticación.
        /// </summary>
        [HttpGet("catalog")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCatalog(int locationId)
        {
            var catalog = await _publicCatalogService.GetCatalogByLocationAsync(locationId);
            return Ok(new ApiOkResponse(catalog));
        }
    }
}
