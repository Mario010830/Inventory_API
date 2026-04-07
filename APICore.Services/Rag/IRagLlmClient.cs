using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace APICore.Services.Rag
{
    public sealed class RagLlmMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public sealed class RagLlmResult
    {
        public string Text { get; set; } = string.Empty;
        public int TokensUsed { get; set; }
        public string Provider { get; set; } = string.Empty;
    }

    public interface IRagLlmClient
    {
        Task<RagLlmResult> CompleteAsync(
            string systemPrompt,
            IReadOnlyList<RagLlmMessage> messages,
            CancellationToken cancellationToken = default);
    }
}
