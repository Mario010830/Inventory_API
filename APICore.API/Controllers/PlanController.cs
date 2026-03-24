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
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Route("api/plan")]
    public class PlanController : Controller
    {
        private readonly IPlanService _planService;
        private readonly IMapper _mapper;

        public PlanController(IPlanService planService, IMapper mapper)
        {
            _planService = planService ?? throw new ArgumentNullException(nameof(planService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetActivePlans()
        {
            var plans = await _planService.GetActivePlansAsync();
            var list = _mapper.Map<List<PlanResponse>>(plans);
            return Ok(new ApiOkResponse(list));
        }

        [Authorize]
        [HttpGet("{id:int}")]
        [RequirePermission(PermissionCodes.PlanRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetById(int id)
        {
            var plan = await _planService.GetByIdAsync(id);
            return Ok(new ApiOkResponse(_mapper.Map<PlanResponse>(plan)));
        }

        [Authorize]
        [HttpPost]
        [RequirePermission(PermissionCodes.PlanManage)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> Create([FromBody] CreateOrUpdatePlanRequest request)
        {
            var plan = await _planService.CreateAsync(request);
            return Created("", new ApiCreatedResponse(_mapper.Map<PlanResponse>(plan)));
        }

        [Authorize]
        [HttpPut("{id:int}")]
        [RequirePermission(PermissionCodes.PlanManage)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> Update(int id, [FromBody] CreateOrUpdatePlanRequest request)
        {
            await _planService.UpdateAsync(id, request);
            return NoContent();
        }

        [Authorize]
        [HttpDelete("{id:int}")]
        [RequirePermission(PermissionCodes.PlanManage)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> Delete(int id)
        {
            await _planService.DeleteAsync(id);
            return NoContent();
        }
    }
}
