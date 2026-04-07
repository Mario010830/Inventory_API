using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using APICore.Services.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace APICore.Services.Rag.Impl
{
    public class RagLlmClient : IRagLlmClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<RagOptions> _options;
        private readonly ILogger<RagLlmClient> _logger;

        public RagLlmClient(
            IHttpClientFactory httpClientFactory,
            IOptions<RagOptions> options,
            ILogger<RagLlmClient> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RagLlmResult> CompleteAsync(
            string systemPrompt,
            IReadOnlyList<RagLlmMessage> messages,
            CancellationToken cancellationToken = default)
        {
            var opt = _options.Value;
            if (string.IsNullOrWhiteSpace(opt.GeminiApiKey))
                throw new InvalidOperationException("Rag:GeminiApiKey no está configurada (requerida para el chat RAG).");

            var model = string.IsNullOrWhiteSpace(opt.GeminiChatModel) ? "gemini-2.0-flash" : opt.GeminiChatModel.Trim();
            var client = _httpClientFactory.CreateClient("GeminiChat");
            var url = $"v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(opt.GeminiApiKey)}";

            var contents = new List<object>();
            foreach (var m in messages)
            {
                if (string.IsNullOrWhiteSpace(m.Content))
                    continue;
                var role = string.Equals(m.Role, "assistant", StringComparison.OrdinalIgnoreCase) ? "model" : "user";
                contents.Add(new
                {
                    role,
                    parts = new[] { new { text = m.Content } }
                });
            }

            if (contents.Count == 0)
                contents.Add(new { role = "user", parts = new[] { new { text = "." } } });

            var payload = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents
            };

            using var response = await client.PostAsJsonAsync(url, payload, cancellationToken).ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini generateContent error {Status}: {Body}", (int)response.StatusCode, json);
                response.EnsureSuccessStatusCode();
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var text = root.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? string.Empty;

            var tokens = 0;
            if (root.TryGetProperty("usageMetadata", out var um) && um.TryGetProperty("totalTokenCount", out var tc))
                tokens = tc.GetInt32();

            return new RagLlmResult { Text = text.Trim(), TokensUsed = tokens, Provider = "gemini" };
        }
    }
}
