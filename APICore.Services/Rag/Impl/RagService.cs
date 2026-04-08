using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Services.Exceptions;
using APICore.Services.Options;
using APICore.Services.Rag;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Pgvector;
using Pgvector.Npgsql;

namespace APICore.Services.Rag.Impl
{
    internal sealed class ManualChunkSearchRow
    {
        public string Content { get; set; } = string.Empty;
        public string SourceFile { get; set; } = string.Empty;
        public double Similarity { get; set; }
    }

    public class RagService : IRagService
    {
        private const string SystemPromptTemplate = @"Eres un asistente de soporte para el sistema de inventario.
Responde ÚNICAMENTE basándote en el contexto del manual provisto.
Si la información no está en el contexto, di ""No encontré información sobre eso en el manual.""
Si el usuario solo saluda o agradece de forma breve, responde de forma natural en el mismo idioma sin forzar citas al manual.
Sé conciso y directo. Responde en el mismo idioma que la pregunta.

Contexto del manual:
{0}";

        private const string SystemPromptWithoutManualChunks = @"Eres un asistente del sistema de inventario.
Para esta pregunta no se recuperaron fragmentos del manual en la base de datos (búsqueda semántica sin resultados o tabla vacía).

Comportamiento:
- Si el usuario solo saluda o es una cortesía breve (hola, buenos días, gracias, etc.), responde de forma natural y breve en el mismo idioma.
- Si pregunta cómo usar el sistema, rutas, permisos o funciones concretas, di con honestidad que ahora mismo no tienes texto del manual en contexto; no inventes pantallas ni URLs. Puedes invitar a reformular la pregunta o revisar el manual en la app.
Sé conciso.";

        private readonly NpgsqlDataSource _dataSource;
        private readonly IEmbeddingService _embedding;
        private readonly IRagLlmClient _llm;
        private readonly IOptions<RagOptions> _options;
        private readonly ILogger<RagService> _logger;

        public RagService(
            NpgsqlDataSource dataSource,
            IEmbeddingService embedding,
            IRagLlmClient llm,
            IOptions<RagOptions> options,
            ILogger<RagService> logger)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _embedding = embedding ?? throw new ArgumentNullException(nameof(embedding));
            _llm = llm ?? throw new ArgumentNullException(nameof(llm));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ChatAskResponse> AskAsync(ChatAskRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var opt = _options.Value;
            if (string.IsNullOrWhiteSpace(request.Question))
                throw new BaseBadRequestException("La pregunta es obligatoria.");

            if (request.Question.Length > opt.MaxQuestionLength)
                throw new BaseBadRequestException($"La pregunta no puede superar {opt.MaxQuestionLength} caracteres.");

            var messages = new List<RagLlmMessage>();
            if (request.ConversationHistory != null)
            {
                foreach (var h in request.ConversationHistory.TakeLast(20))
                {
                    if (h == null || string.IsNullOrWhiteSpace(h.Content))
                        continue;
                    if (!string.Equals(h.Role, "user", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(h.Role, "assistant", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var role = h.Role.ToLowerInvariant();
                    messages.Add(new RagLlmMessage { Role = role, Content = h.Content.Trim() });
                }
            }

            messages.Add(new RagLlmMessage { Role = "user", Content = request.Question.Trim() });

            var queryVector = await _embedding.EmbedAsync(request.Question, cancellationToken).ConfigureAwait(false);
            var topK = Math.Clamp(opt.TopKChunks, 1, 50);
            if (topK != opt.TopKChunks)
                _logger.LogWarning("Rag:TopKChunks ajustado de {Original} a {Usado} (debe estar entre 1 y 50).", opt.TopKChunks, topK);

            var rows = await SearchSimilarAsync(queryVector, topK, cancellationToken).ConfigureAwait(false);

            string systemPrompt;
            if (rows.Count == 0)
            {
                _logger.LogInformation(
                    "RAG: 0 fragmentos del manual; se responde con el modelo sin contexto (revisa ingesta y ConnectionStrings:ApiConnection si esperabas datos).");
                systemPrompt = SystemPromptWithoutManualChunks;
            }
            else
            {
                var context = BuildContextBlock(rows);
                systemPrompt = string.Format(SystemPromptTemplate, context);
            }

            var result = await _llm.CompleteAsync(systemPrompt, messages, cancellationToken).ConfigureAwait(false);

            var sources = rows.Count == 0
                ? new List<string>()
                : rows
                    .Select(r => r.SourceFile)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();

            return new ChatAskResponse
            {
                Answer = result.Text,
                Sources = sources,
                TokensUsed = result.TokensUsed
            };
        }

        private async Task<List<ManualChunkSearchRow>> SearchSimilarAsync(float[] query, int topK, CancellationToken cancellationToken)
        {
            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = new NpgsqlCommand(
                @"SELECT content, source_file, 1 - (embedding <=> @q) AS similarity
                  FROM manual_chunks
                  ORDER BY embedding <=> @q
                  LIMIT @k",
                conn);
            cmd.Parameters.Add(NpgsqlVectorParameter.Create("q", new Vector(query)));
            cmd.Parameters.AddWithValue("k", topK);

            var list = new List<ManualChunkSearchRow>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                list.Add(new ManualChunkSearchRow
                {
                    Content = reader.GetString(0),
                    SourceFile = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Similarity = reader.GetDouble(2)
                });
            }

            return list;
        }

        private static string BuildContextBlock(IReadOnlyList<ManualChunkSearchRow> rows)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                sb.AppendLine($"--- Fragmento {i + 1} (fuente: {r.SourceFile}) ---");
                sb.AppendLine(r.Content.Trim());
                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }
    }
}
