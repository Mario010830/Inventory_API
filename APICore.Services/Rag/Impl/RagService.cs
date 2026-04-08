using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public int ChunkIndex { get; set; }
        public double Similarity { get; set; }
    }

    public class RagService : IRagService
    {
        private const string SystemPromptTemplate = @"Eres un asistente de soporte para el sistema de inventario.

Tienes fragmentos del manual debajo. Tu trabajo es ayudar al usuario usando ESE texto.
- Si el fragmento habla del tema de la pregunta aunque use otras palabras (ej. ""inventario"", ""stock"", ""Movimientos"", rutas `/dashboard/...`), responde con pasos o datos concretos del contexto. Puedes relacionar sinónimos y secciones cercanas.
- No inventes pantallas, URLs ni permisos que no aparezcan en el contexto.
- Solo di exactamente: ""No encontré información sobre eso en el manual."" cuando el contexto no contiene NADA útil ni indirectamente relacionado con lo preguntado.
- Saludos o gracias breves: responde de forma natural en el mismo idioma, sin forzar citas.

Sé conciso. Mismo idioma que la pregunta.

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
            if (rows.Count == 0)
            {
                rows = await SearchLexicalFallbackAsync(request.Question.Trim(), topK, cancellationToken).ConfigureAwait(false);
                if (rows.Count > 0)
                    _logger.LogInformation(
                        "RAG: búsqueda vectorial sin filas; rescate por palabras clave devolvió {Count} fragmento(s).",
                        rows.Count);
            }

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
                @"SELECT content, source_file, chunk_index, 1 - (embedding <=> @q) AS similarity
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
                    ChunkIndex = reader.GetInt32(2),
                    Similarity = reader.GetDouble(3)
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

        /// <summary>Cuando la similitud vectorial no devuelve filas (p. ej. pregunta en español vs embeddings), busca coincidencias de palabras en el texto.</summary>
        private async Task<List<ManualChunkSearchRow>> SearchLexicalFallbackAsync(
            string question,
            int topK,
            CancellationToken cancellationToken)
        {
            var terms = CollectLexicalTerms(question);
            if (terms.Count == 0)
                return new List<ManualChunkSearchRow>();

            await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var sb = new StringBuilder();
            sb.Append("SELECT content, source_file, chunk_index, 0.45::float8 AS similarity FROM manual_chunks WHERE ");
            await using var cmd = new NpgsqlCommand { Connection = conn };
            for (var i = 0; i < terms.Count; i++)
            {
                if (i > 0)
                    sb.Append(" OR ");
                sb.Append($"content ILIKE @t{i} ESCAPE '\\'");
                cmd.Parameters.Add(new NpgsqlParameter($"t{i}", "%" + EscapeLikePattern(terms[i]) + "%"));
            }

            sb.Append(" ORDER BY id LIMIT @lim");
            cmd.Parameters.AddWithValue("lim", Math.Clamp(topK * 15, 30, 200));
            cmd.CommandText = sb.ToString();

            var raw = new List<ManualChunkSearchRow>();
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                raw.Add(new ManualChunkSearchRow
                {
                    Content = reader.GetString(0),
                    SourceFile = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    ChunkIndex = reader.GetInt32(2),
                    Similarity = reader.GetDouble(3)
                });
            }

            return raw
                .GroupBy(r => (r.SourceFile, r.ChunkIndex))
                .Select(g => g.First())
                .Take(topK)
                .ToList();
        }

        private static readonly HashSet<string> LexicalStopwords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "for", "are", "but", "not", "you", "all", "can", "her", "was", "one", "our", "out", "day", "get", "has", "him", "his", "how", "its", "may", "new", "now", "old", "see", "two", "way", "who", "boy", "did", "let", "put", "say", "she", "too", "use",
            "el", "la", "los", "las", "un", "una", "unos", "unas", "de", "del", "al", "y", "o", "u", "a", "en", "por", "para", "que", "con", "sin", "se", "es", "son", "soy", "era", "fue", "han", "hay", "les", "le", "lo", "me", "mi", "mis", "tu", "tus", "su", "sus", "ya", "muy", "más", "menos", "este", "esta", "esto", "ese", "esa", "eso", "aquí", "allí", "donde", "cuando", "como", "cual", "quien",
            "hola", "buenos", "dias", "tardes", "noches", "gracias", "favor", "solo", "tan", "tanto",
            "cómo", "dónde", "qué", "cuál", "quién", "está", "están", "tengo", "tienes", "tiene"
        };

        private static List<string> CollectLexicalTerms(string question)
        {
            var found = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in Regex.Matches(question, @"[\p{L}]{3,}", RegexOptions.CultureInvariant))
            {
                var w = m.Value;
                if (w.Length < 3)
                    continue;
                var lower = w.ToLowerInvariant();
                if (LexicalStopwords.Contains(lower))
                    continue;
                foreach (var expanded in ExpandLexicalTerm(w))
                {
                    if (expanded.Length < 3)
                        continue;
                    if (seen.Add(expanded))
                        found.Add(expanded);
                    if (found.Count >= 14)
                        return found;
                }
            }

            return found;
        }

        private static IEnumerable<string> ExpandLexicalTerm(string word)
        {
            yield return word;
            var lower = word.ToLowerInvariant();
            switch (lower)
            {
                case "creo":
                case "crea":
                case "creamos":
                case "creado":
                case "creada":
                case "creating":
                case "create":
                case "created":
                    yield return "crear";
                    break;
                case "productos":
                    yield return "producto";
                    break;
                case "inventarios":
                    yield return "inventario";
                    break;
                case "movimientos":
                    yield return "movimiento";
                    break;
                case "ubicaciones":
                    yield return "ubicación";
                    yield return "ubicacion";
                    break;
                case "categorias":
                case "categorías":
                    yield return "categoría";
                    yield return "categoria";
                    break;
                case "proveedores":
                    yield return "proveedor";
                    break;
                case "ventas":
                    yield return "venta";
                    break;
                case "contactos":
                    yield return "contacto";
                    break;
                case "usuarios":
                    yield return "usuario";
                    break;
                case "roles":
                    yield return "rol";
                    break;
            }
        }

        private static string EscapeLikePattern(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            return s.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
        }
    }
}
