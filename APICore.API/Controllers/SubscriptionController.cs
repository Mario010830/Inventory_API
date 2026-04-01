using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.API.Utils;
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
    [Authorize]
    [Route("api/subscription")]
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ICurrentUserContextAccessor _currentUserContextAccessor;
        private readonly IMapper _mapper;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            ICurrentUserContextAccessor currentUserContextAccessor,
            IMapper mapper)
        {
            _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
            _currentUserContextAccessor = currentUserContextAccessor ?? throw new ArgumentNullException(nameof(currentUserContextAccessor));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>Todas las suscripciones (panel superadmin). Filtros opcionales: status, planId.</summary>
        [HttpGet]
        [RequirePermission(PermissionCodes.SubscriptionManage)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAll(int? page, int? perPage, string status = null, int? planId = null)
        {
            var result = await _subscriptionService.GetAllSubscriptionsAsync(page, perPage, status, planId);
            var list = _mapper.Map<List<SubscriptionResponse>>(result);
            return Ok(new ApiOkPaginatedResponse(list, result.GetPaginationData));
        }

        /// <summary>Detalle de una suscripción con organización y plan.</summary>
        [HttpGet("{id:int}")]
        [RequirePermission(PermissionCodes.SubscriptionManage)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var sub = await _subscriptionService.GetSubscriptionByIdAsync(id);
            return Ok(new ApiOkResponse(_mapper.Map<SubscriptionResponse>(sub)));
        }

        [HttpGet("my-subscription")]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetMySubscription()
        {
            var ctx = _currentUserContextAccessor.GetCurrent();
            if (ctx?.OrganizationId == null)
                return NotFound(new ApiResponse(404, "El usuario no pertenece a una organización."));

            var sub = await _subscriptionService.GetMySubscriptionAsync(ctx.OrganizationId.Value);
            return Ok(new ApiOkResponse(sub));
        }

        [HttpGet("requests")]
        [RequirePermission(PermissionCodes.SubscriptionManage)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetRequests(int? page, int? perPage, string status = null)
        {
            var result = await _subscriptionService.GetSubscriptionRequestsAsync(page, perPage, status);
            var list = _mapper.Map<List<SubscriptionRequestResponse>>(result);
            return Ok(new ApiOkPaginatedResponse(list, result.GetPaginationData));
        }

        [HttpGet("requests/{id:int}")]
        [RequirePermission(PermissionCodes.SubscriptionManage)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetRequestById(int id)
        {
            var req = await _subscriptionService.GetSubscriptionRequestByIdAsync(id);
            return Ok(new ApiOkResponse(_mapper.Map<SubscriptionRequestResponse>(req)));
        }

        [HttpPost("requests/{id:int}/approve")]
        [RequirePermission(PermissionCodes.SubscriptionManage)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ApproveRequest(int id, [FromBody] ApproveSubscriptionRequestDto dto)
        {
            var reviewerId = User.GetUserIdFromToken();
            var req = await _subscriptionService.ApproveRequestAsync(id, dto ?? new ApproveSubscriptionRequestDto(), reviewerId);
            return Ok(new ApiOkResponse(_mapper.Map<SubscriptionRequestResponse>(req)));
        }

        [HttpPost("requests/{id:int}/reject")]
        [RequirePermission(PermissionCodes.SubscriptionManage)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> RejectRequest(int id, [FromBody] RejectSubscriptionRequestDto dto)
        {
            var reviewerId = User.GetUserIdFromToken();
            var req = await _subscriptionService.RejectRequestAsync(id, dto, reviewerId);
            return Ok(new ApiOkResponse(_mapper.Map<SubscriptionRequestResponse>(req)));
        }

        /// <summary>Cancela una suscripción ya activa (distinto de rechazar solicitud pendiente). La organización queda inactiva.</summary>
        [HttpPost("{id:int}/cancel")]
        [RequirePermission(PermissionCodes.SubscriptionManage)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CancelSubscription(int id, [FromBody] CancelSubscriptionRequestDto dto)
        {
            var reviewerId = User.GetUserIdFromToken();
            var sub = await _subscriptionService.CancelSubscriptionAsync(id, dto, reviewerId);
            return Ok(new ApiOkResponse(_mapper.Map<SubscriptionResponse>(sub)));
        }

        [HttpPost("{id:int}/renew")]
        [RequirePermission(PermissionCodes.SubscriptionManage)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Renew(int id, [FromBody] RenewSubscriptionRequest dto)
        {
            var reviewerId = User.GetUserIdFromToken();
            var sub = await _subscriptionService.RenewSubscriptionAsync(id, dto, reviewerId);
            return Ok(new ApiOkResponse(_mapper.Map<SubscriptionResponse>(sub)));
        }

        [HttpPut("{id:int}/change-plan")]
        [RequirePermission(PermissionCodes.SubscriptionManage)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ChangePlan(int id, [FromBody] ChangePlanRequest dto)
        {
            var reviewerId = User.GetUserIdFromToken();
            var sub = await _subscriptionService.ChangePlanAsync(id, dto, reviewerId);
            return Ok(new ApiOkResponse(_mapper.Map<SubscriptionResponse>(sub)));
        }
    }
}
