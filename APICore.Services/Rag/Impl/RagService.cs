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
Sé conciso y directo. Responde en el mismo idioma que la pregunta.

Contexto del manual:
{0}";

        private readonly NpgsqlDataSource _dataSource;
        private readonly IEmbeddingService _embedding;
        private readonly IRagLlmClient _llm;
        private readonly IOptions<RagOptions> _options;

        public RagService(
            NpgsqlDataSource dataSource,
            IEmbeddingService embedding,
            IRagLlmClient llm,
            IOptions<RagOptions> options)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _embedding = embedding ?? throw new ArgumentNullException(nameof(embedding));
            _llm = llm ?? throw new ArgumentNullException(nameof(llm));
            _options = options ?? throw new ArgumentNullException(nameof(options));
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

            var queryVector = await _embedding.EmbedAsync(request.Question, cancellationToken).ConfigureAwait(false);
            var rows = await SearchSimilarAsync(queryVector, opt.TopKChunks, cancellationToken).ConfigureAwait(false);

            if (rows.Count == 0)
            {
                return new ChatAskResponse
                {
                    Answer = "No encontré información sobre eso en el manual.",
                    Sources = Array.Empty<string>(),
                    TokensUsed = 0
                };
            }

            var context = BuildContextBlock(rows);
            var systemPrompt = string.Format(SystemPromptTemplate, context);

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

            var result = await _llm.CompleteAsync(systemPrompt, messages, cancellationToken).ConfigureAwait(false);

            var sources = rows
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
            cmd.Parameters.AddWithValue("q", new Vector(query));
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
