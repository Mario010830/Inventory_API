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
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Authorize]
    [Route("api/location")]
    public class LocationController : Controller
    {
        private readonly ILocationService _locationService;

        public LocationController(ILocationService locationService)
        {
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.LocationRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLocations(int? page, int? perPage, [FromQuery] int? organizationId = null, string sortOrder = null)
        {
            var locations = await _locationService.GetAllLocations(page, perPage, organizationId, sortOrder);
            return Ok(new ApiOkPaginatedResponse(locations, locations.GetPaginationData));
        }

        [HttpGet("id")]
        [RequirePermission(PermissionCodes.LocationRead)]
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
