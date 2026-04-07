using System.Threading;
using System.Threading.Tasks;
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;

namespace APICore.Services.Rag
{
    public interface IRagService
    {
        Task<ChatAskResponse> AskAsync(ChatAskRequest request, CancellationToken cancellationToken = default);
    }
}
