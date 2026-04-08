using System;
using System.Collections.Generic;
using System.Linq;
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
    public class GeminiEmbeddingService : IEmbeddingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<RagOptions> _options;
        private readonly ILogger<GeminiEmbeddingService> _logger;

        public GeminiEmbeddingService(
            IHttpClientFactory httpClientFactory,
            IOptions<RagOptions> options,
            ILogger<GeminiEmbeddingService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("El texto no puede estar vacío.", nameof(text));

            var opt = _options.Value;
            if (string.IsNullOrWhiteSpace(opt.GeminiApiKey))
                throw new InvalidOperationException("Rag:GeminiApiKey no está configurada (requerida para embeddings).");

            var model = opt.EmbeddingModel.Trim();
            var client = _httpClientFactory.CreateClient("GeminiEmbedding");
            var url = $"v1beta/models/{Uri.EscapeDataString(model)}:embedContent?key={Uri.EscapeDataString(opt.GeminiApiKey)}";

            // Claves en snake_case: System.Text.Json camelCase no sirve para output_dimensionality.
            var payload = new Dictionary<string, object?>
            {
                ["model"] = $"models/{model}",
                ["content"] = new Dictionary<string, object?>
                {
                    ["parts"] = new[] { new Dictionary<string, string> { ["text"] = text } }
                }
            };
            if (model.StartsWith("gemini-embedding", StringComparison.OrdinalIgnoreCase))
                payload["output_dimensionality"] = opt.EmbeddingDimension;

            using var response = await client.PostAsJsonAsync(url, payload, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini embed falló: {Status} {Body}", (int)response.StatusCode, body);
                response.EnsureSuccessStatusCode();
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Respuesta típica: { "embedding": { "values": [ ... ] } }
            if (!root.TryGetProperty("embedding", out var emb))
            {
                _logger.LogError("Respuesta embed sin 'embedding': {Body}", body);
                throw new InvalidOperationException("Respuesta de embedding inválida.");
            }

            if (!emb.TryGetProperty("values", out var values) || values.ValueKind != JsonValueKind.Array)
            {
                _logger.LogError("Respuesta embed sin 'values': {Body}", body);
                throw new InvalidOperationException("Respuesta de embedding inválida (values).");
            }

            var floats = values.EnumerateArray()
                .Select(e => (float)e.GetDouble())
                .ToArray();

            if (floats.Length != opt.EmbeddingDimension)
                _logger.LogWarning(
                    "Dimensión de embedding {Actual} distinta a Rag:EmbeddingDimension {Esperada}. Ajusta la configuración o el modelo.",
                    floats.Length, opt.EmbeddingDimension);

            return floats;
        }
    }
}
