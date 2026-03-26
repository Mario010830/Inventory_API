using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.API.Services;
using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Route("api/push")]
    public class PushController : Controller
    {
        private readonly IPushNotificationService _pushNotificationService;

        public PushController(IPushNotificationService pushNotificationService)
        {
            _pushNotificationService = pushNotificationService ?? throw new ArgumentNullException(nameof(pushNotificationService));
        }

        [HttpPost("subscribe")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Subscribe([FromBody] PushSubscribeRequest request)
        {
            await _pushNotificationService.UpsertSubscriptionAsync(request);
            return Ok(new ApiOkResponse(new PushOperationResponse { Success = true }));
        }

        [HttpPost("send")]
        [Authorize]
        [RequirePermission(PermissionCodes.ProductUpdate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Send([FromBody] PushSendRequest request)
        {
            var result = await _pushNotificationService.SendToLocationAsync(request);
            return Ok(new ApiOkResponse(result));
        }
    }
}
