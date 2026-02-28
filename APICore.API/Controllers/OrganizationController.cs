using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Authorize]
    [Route("api/organization")]
    public class OrganizationController : Controller
    {
        private readonly IOrganizationService _organizationService;

        public OrganizationController(IOrganizationService organizationService)
        {
            _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.OrganizationRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetOrganizations(int? page, int? perPage, string sortOrder = null)
        {
            var organizations = await _organizationService.GetAllOrganizations(page, perPage, sortOrder);
            return Ok(new ApiOkPaginatedResponse(organizations, organizations.GetPaginationData));
        }

        [HttpGet("id")]
        [RequirePermission(PermissionCodes.OrganizationRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetOrganizationById(int id)
        {
            var organization = await _organizationService.GetOrganization(id);
            return Ok(new ApiOkResponse(organization));
        }

        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request)
        {
            var organization = await _organizationService.CreateOrganization(request);
            return Created("", new ApiCreatedResponse(organization));
        }

        [HttpPut]
        [RequirePermission(PermissionCodes.OrganizationUpdate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateOrganization(int id, [FromBody] UpdateOrganizationRequest request)
        {
            await _organizationService.UpdateOrganization(id, request);
            return NoContent();
        }

        [HttpDelete]
        [RequirePermission(PermissionCodes.OrganizationDelete)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> DeleteOrganization(int id)
        {
            await _organizationService.DeleteOrganization(id);
            return NoContent();
        }
    }
}
