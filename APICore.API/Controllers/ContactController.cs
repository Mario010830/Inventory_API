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
    [Route("api/contact")]
    public class ContactController : Controller
    {
        private readonly IContactService _contactService;
        private readonly ILoyaltyService _loyaltyService;
        private readonly IMapper _mapper;

        public ContactController(IContactService contactService, ILoyaltyService loyaltyService, IMapper mapper)
        {
            _contactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
            _loyaltyService = loyaltyService ?? throw new ArgumentNullException(nameof(loyaltyService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.ContactCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateContact([FromBody] CreateContactRequest request)
        {
            var result = await _contactService.CreateContact(request);
            var response = _mapper.Map<ContactResponse>(result);
            return Created("", new ApiCreatedResponse(response));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.ContactRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetContacts(int? page, int? perPage, string sortOrder = null, string? role = null)
        {
            var contacts = await _contactService.GetAllContacts(page, perPage, sortOrder, role);
            var list = _mapper.Map<IEnumerable<ContactResponse>>(contacts);
            return Ok(new ApiOkPaginatedResponse(list, contacts.GetPaginationData));
        }

        [HttpPost("counterparty")]
        [RequirePermission(PermissionCodes.ContactCreate, PermissionCodes.SupplierCreate, PermissionCodes.LeadCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateCounterparty([FromBody] CreateCounterpartyRequest request)
        {
            var result = await _contactService.CreateCounterparty(request);
            var response = _mapper.Map<ContactResponse>(result);
            return Created("", new ApiCreatedResponse(response));
        }

        [HttpGet("{id:int}/loyalty")]
        [RequirePermission(PermissionCodes.ContactRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetContactLoyalty(int id)
        {
            var summary = await _loyaltyService.GetLoyaltySummaryForContactAsync(id);
            return Ok(new ApiOkResponse(summary));
        }

        [HttpGet("{id:int}")]
        [RequirePermission(PermissionCodes.ContactRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetContactById(int id)
        {
            var contact = await _contactService.GetContact(id);
            var response = _mapper.Map<ContactResponse>(contact);
            return Ok(new ApiOkResponse(response));
        }

        [HttpPut]
        [RequirePermission(PermissionCodes.ContactUpdate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> EditContact(int id, [FromBody] UpdateContactRequest request)
        {
            await _contactService.UpdateContact(id, request);
            return NoContent();
        }

        [HttpDelete]
        [RequirePermission(PermissionCodes.ContactDelete)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteContact(int id)
        {
            await _contactService.DeleteContact(id);
            return NoContent();
        }
    }
}
