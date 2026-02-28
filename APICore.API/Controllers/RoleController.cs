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
    [Route("api/role")]
    public class RoleController : Controller
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        }

        [HttpGet("permissions")]
        [RequirePermission(PermissionCodes.RoleRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAllPermissions()
        {
            var list = await _roleService.GetAllPermissions();
            return Ok(new ApiOkResponse(list));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.RoleRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetRoles(int? page, int? perPage, string sortOrder = null)
        {
            var roles = await _roleService.GetAllRoles(page, perPage, sortOrder);
            return Ok(new ApiOkPaginatedResponse(roles, roles.GetPaginationData));
        }

        [HttpGet("id")]
        [RequirePermission(PermissionCodes.RoleRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetRoleById(int id)
        {
            var role = await _roleService.GetRole(id);
            return Ok(new ApiOkResponse(role));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.RoleCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
        {
            var role = await _roleService.CreateRole(request);
            return Created("", new ApiCreatedResponse(role));
        }

        [HttpPut]
        [RequirePermission(PermissionCodes.RoleUpdate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
        {
            await _roleService.UpdateRole(id, request);
            return NoContent();
        }

        [HttpDelete]
        [RequirePermission(PermissionCodes.RoleDelete)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> DeleteRole(int id)
        {
            await _roleService.DeleteRole(id);
            return NoContent();
        }
    }
}
