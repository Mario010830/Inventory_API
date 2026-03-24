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
    [Route("api/business-category")]
    public class BusinessCategoryController : Controller
    {
        private readonly IBusinessCategoryService _businessCategoryService;

        public BusinessCategoryController(IBusinessCategoryService businessCategoryService)
        {
            _businessCategoryService = businessCategoryService ?? throw new ArgumentNullException(nameof(businessCategoryService));
        }

        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetActive()
        {
            var items = await _businessCategoryService.GetActiveAsync();
            return Ok(new ApiOkResponse(items));
        }

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _businessCategoryService.GetByIdAsync(id);
            return Ok(new ApiOkResponse(item));
        }

        [Authorize]
        [HttpPost]
        [RequirePermission(PermissionCodes.Admin)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateBusinessCategoryRequest request)
        {
            var item = await _businessCategoryService.CreateAsync(request);
            return Created("", new ApiCreatedResponse(item));
        }

        [Authorize]
        [HttpPut("{id:int}")]
        [RequirePermission(PermissionCodes.Admin)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateBusinessCategoryRequest request)
        {
            var item = await _businessCategoryService.UpdateAsync(id, request);
            return Ok(new ApiOkResponse(item));
        }

        [Authorize]
        [HttpDelete("{id:int}")]
        [RequirePermission(PermissionCodes.Admin)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _businessCategoryService.DeleteAsync(id);
            return NoContent();
        }
    }
}
