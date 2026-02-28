using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Authorize]
    [Route("api/lead")]
    public class LeadController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly IMapper _mapper;

        public LeadController(ILeadService leadService, IMapper mapper)
        {
            _leadService = leadService ?? throw new ArgumentNullException(nameof(leadService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.LeadCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateLead([FromBody] CreateLeadRequest request)
        {
            var result = await _leadService.CreateLead(request);
            var response = _mapper.Map<LeadResponse>(result);
            return Created("", new ApiCreatedResponse(response));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.LeadRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetLeads(int? page, int? perPage, string? status = null, string sortOrder = null)
        {
            var leads = await _leadService.GetAllLeads(page, perPage, status, sortOrder);
            var list = _mapper.Map<IEnumerable<LeadResponse>>(leads);
            return Ok(new ApiOkPaginatedResponse(list, leads.GetPaginationData));
        }

        [HttpGet("id")]
        [RequirePermission(PermissionCodes.LeadRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetLeadById(int id)
        {
            var lead = await _leadService.GetLead(id);
            var response = _mapper.Map<LeadResponse>(lead);
            return Ok(new ApiOkResponse(response));
        }

        [HttpPut]
        [RequirePermission(PermissionCodes.LeadUpdate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> EditLead(int id, [FromBody] UpdateLeadRequest request)
        {
            await _leadService.UpdateLead(id, request);
            return NoContent();
        }

        [HttpDelete]
        [RequirePermission(PermissionCodes.LeadDelete)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteLead(int id)
        {
            await _leadService.DeleteLead(id);
            return NoContent();
        }

        [HttpPost("{id}/convert")]
        [RequirePermission(PermissionCodes.LeadUpdate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> ConvertToContact(int id)
        {
            var contact = await _leadService.ConvertToContact(id);
            var response = _mapper.Map<ContactResponse>(contact);
            return Created("", new ApiCreatedResponse(response));
        }
    }
}
