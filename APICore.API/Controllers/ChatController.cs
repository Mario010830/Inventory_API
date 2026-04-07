using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Services.Options;
using APICore.Services.Rag;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace APICore.API.Controllers
{
    [Authorize]
    [Route("api/chat")]
    public class ChatController : Controller
    {
        private readonly IRagService _ragService;
        private readonly IOptions<RagOptions> _ragOptions;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            IRagService ragService,
            IOptions<RagOptions> ragOptions,
            ILogger<ChatController> logger)
        {
            _ragService = ragService ?? throw new ArgumentNullException(nameof(ragService));
            _ragOptions = ragOptions ?? throw new ArgumentNullException(nameof(ragOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Pregunta al asistente RAG sobre el manual de usuario.</summary>
        [HttpPost("ask")]
        [RequirePermission(PermissionCodes.ManualChatAsk)]
        [ProducesResponseType(typeof(ApiOkResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiBadRequestResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Ask([FromBody] ChatAskRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                return BadRequest(new ApiBadRequestResponse("Cuerpo de solicitud inválido."));

            if (!ModelState.IsValid)
                return BadRequest(new ApiBadRequestResponse(ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Validación incorrecta."));

            var maxLen = _ragOptions.Value.MaxQuestionLength;
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new ApiBadRequestResponse("La pregunta es obligatoria."));
            if (request.Question.Length > maxLen)
                return BadRequest(new ApiBadRequestResponse($"La pregunta no puede superar {maxLen} caracteres."));

            if (request.ConversationHistory != null)
            {
                foreach (var turn in request.ConversationHistory)
                {
                    if (turn == null || string.IsNullOrWhiteSpace(turn.Role))
                        continue;
                    var r = turn.Role.Trim();
                    if (!string.Equals(r, "user", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(r, "assistant", StringComparison.OrdinalIgnoreCase))
                        return BadRequest(new ApiBadRequestResponse("conversationHistory: cada rol debe ser user o assistant."));
                }
            }

            var response = await _ragService.AskAsync(request, cancellationToken).ConfigureAwait(false);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.LogInformation(
                "RagChat completado: remoteIp={RemoteIp} tokensUsed={TokensUsed} sourceCount={SourceCount}",
                ip,
                response.TokensUsed,
                response.Sources?.Count ?? 0);

            return Ok(new ApiOkResponse(response));
        }
    }
}
