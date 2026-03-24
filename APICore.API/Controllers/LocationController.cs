using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Authorize]
    [Route("api/location")]
    public class LocationController : Controller
    {
        private readonly ILocationService _locationService;
        private readonly IStorageService _storageService;

        public LocationController(ILocationService locationService, IStorageService storageService)
        {
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        /// <summary>
        /// Sube la foto de una ubicación. Devuelve la URL para enviar en PhotoUrl al crear/editar ubicación.
        /// </summary>
        [HttpPost("image")]
        [Consumes("multipart/form-data")]
        [Produces("application/json")]
        [RequirePermission(PermissionCodes.LocationCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UploadLocationImage(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new ApiBadRequestResponse("Debe enviar un archivo de imagen."));

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new ApiBadRequestResponse("Solo se permiten imágenes: JPEG, PNG, GIF, WebP."));

            if (file.Length > 5 * 1024 * 1024) // 5 MB
                return BadRequest(new ApiBadRequestResponse("El archivo no debe superar 5 MB."));

            using var stream = file.OpenReadStream();
            var url = await _storageService.UploadLocationImageAsync(stream, file.FileName, file.ContentType);
            return Ok(new ApiOkResponse(new { PhotoUrl = url }));
        }

       
        [HttpGet]
        [RequirePermission(PermissionCodes.LocationRead, PermissionCodes.LocationCreate, PermissionCodes.LocationUpdate, PermissionCodes.InventoryMovementCreate, PermissionCodes.SaleCreate, PermissionCodes.InventoryRead, PermissionCodes.InventoryManage)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLocations(int? page, int? perPage, [FromQuery] int? organizationId = null, string sortOrder = null)
        {
            var locations = await _locationService.GetAllLocations(page, perPage, organizationId, sortOrder);
            return Ok(new ApiOkPaginatedResponse(locations, locations.GetPaginationData));
        }

       
        [HttpGet("id")]
        [RequirePermission(PermissionCodes.LocationRead, PermissionCodes.LocationCreate, PermissionCodes.LocationUpdate, PermissionCodes.InventoryMovementCreate, PermissionCodes.SaleCreate, PermissionCodes.InventoryRead, PermissionCodes.InventoryManage)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetLocationById(int id)
        {
            var location = await _locationService.GetLocation(id);
            return Ok(new ApiOkResponse(location));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.LocationCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest request)
        {
            var location = await _locationService.CreateLocation(request);
            return Created("", new ApiCreatedResponse(location));
        }

        [HttpPut]
        [RequirePermission(PermissionCodes.LocationUpdate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] UpdateLocationRequest request)
        {
            await _locationService.UpdateLocation(id, request);
            return NoContent();
        }

        [HttpDelete]
        [RequirePermission(PermissionCodes.LocationDelete)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> DeleteLocation(int id)
        {
            await _locationService.DeleteLocation(id);
            return NoContent();
        }
    }
}
