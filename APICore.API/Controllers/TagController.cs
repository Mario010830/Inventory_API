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
    [Route("api/tags")]
    public class TagController : Controller
    {
        private readonly ITagService _tagService;

        public TagController(ITagService tagService)
        {
            _tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));
        }

        /// <summary>Lista todas las etiquetas globales (admin).</summary>
        [HttpGet]
        [RequirePermission(PermissionCodes.TagRead, PermissionCodes.TagCreate, PermissionCodes.TagUpdate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAll()
        {
            var tags = await _tagService.GetAllAsync();
            return Ok(new ApiOkResponse(tags));
        }

        /// <summary>Obtiene una etiqueta por id.</summary>
        [HttpGet("{id:int}")]
        [RequirePermission(PermissionCodes.TagRead, PermissionCodes.TagCreate, PermissionCodes.TagUpdate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var tag = await _tagService.GetByIdAsync(id);
            return Ok(new ApiOkResponse(tag));
        }

        /// <summary>Crea una nueva etiqueta. Slug se genera automáticamente desde el nombre.</summary>
        [HttpPost]
        [RequirePermission(PermissionCodes.TagCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateTagRequest request)
        {
            var tag = await _tagService.CreateAsync(request);
            return Created("", new ApiCreatedResponse(tag));
        }

        /// <summary>Actualiza nombre y/o color. Si cambia el nombre, se regenera el slug.</summary>
        [HttpPut("{id:int}")]
        [RequirePermission(PermissionCodes.TagUpdate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTagRequest request)
        {
            var tag = await _tagService.UpdateAsync(id, request);
            return Ok(new ApiOkResponse(tag));
        }

        /// <summary>Elimina la etiqueta. Error 400 si tiene productos asignados.</summary>
        [HttpDelete("{id:int}")]
        [RequirePermission(PermissionCodes.TagDelete)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _tagService.DeleteAsync(id);
            return NoContent();
        }
    }
}
