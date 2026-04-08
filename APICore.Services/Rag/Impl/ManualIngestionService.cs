using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using APICore.Services.Options;
using APICore.Services.Rag;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Pgvector;
using Pgvector.Npgsql;

namespace APICore.Services.Rag.Impl
{
    public class ManualIngestionService : IManualIngestionService
    {
        private const int InsertBatchSize = 12;

        private readonly NpgsqlDataSource _dataSource;
        private readonly IEmbeddingService _embedding;
        private readonly IOptions<RagOptions> _options;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ILogger<ManualIngestionService> _logger;

        public ManualIngestionService(
            NpgsqlDataSource dataSource,
            IEmbeddingService embedding,
            IOptions<RagOptions> options,
            IHostEnvironment hostEnvironment,
            ILogger<ManualIngestionService> logger)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _embedding = embedding ?? throw new ArgumentNullException(nameof(embedding));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>ContentRoot + ruta relativa (p. ej. publish + manual → publish/manual), o ruta absoluta.</summary>
        private string ResolveManualRootPath(string configured)
        {
            var mp = string.IsNullOrWhiteSpace(configured) ? "manual" : configured.Trim();

            if (Path.IsPathRooted(mp))
                return Path.GetFullPath(mp);

            while (mp.StartsWith("./", StringComparison.Ordinal) || mp.StartsWith(".\\", StringComparison.Ordinal))
                mp = mp[2..];
            mp = mp.TrimStart('/', '\\');

            return Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, mp));
        }

        public async Task<ManualIngestionSummary> IngestManualAsync(CancellationToken cancellationToken = default)
        {
            var opt = _options.Value;
            var root = ResolveManualRootPath(opt.ManualPath);
            if (!Directory.Exists(root))
            {
                _logger.LogWarning(
                    "No existe la carpeta del manual: {Path} (ContentRoot={ContentRoot}, Rag:ManualPath={Configured})",
                    root,
                    _hostEnvironment.ContentRootPath,
                    opt.ManualPath);
                return new ManualIngestionSummary();
            }

            _logger.LogInformation("Ingesta manual desde: {Path}", root);

            var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    var ext = Path.GetExtension(f);
                    return string.Equals(ext, ".md", StringComparison.OrdinalIgnoreCase)
                           || string.Equals(ext, ".txt", StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var summary = new ManualIngestionSummary();
            var delayMs = Math.Max(0, opt.IngestionDelayMsBetweenChunks);

            foreach (var fullPath in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relative = Path.GetRelativePath(root, fullPath).Replace('\\', '/');
                if (relative.Length > 255)
                {
                    _logger.LogWarning("Ruta relativa demasiado larga (>255), se omite: {File}", fullPath);
                    continue;
                }

                var text = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
                var chunkTexts = ManualTextChunker.ChunkText(text);
                _logger.LogInformation("Archivo {File}: {Count} fragmentos", relative, chunkTexts.Count);

                await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
                await using var tx = await conn.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await DeleteChunksForSourceInTransactionAsync(conn, tx, relative, cancellationToken).ConfigureAwait(false);

                    var pending = new List<ChunkRow>(InsertBatchSize);
                    for (var i = 0; i < chunkTexts.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var content = chunkTexts[i];
                        var vector = await _embedding.EmbedAsync(content, cancellationToken).ConfigureAwait(false);
                        var meta = JsonSerializer.Serialize(new { file = relative, index = i });
                        pending.Add(new ChunkRow(relative, i, content, vector, meta));
                        summary.ChunksWritten++;

                        if (pending.Count >= InsertBatchSize)
                        {
                            await InsertBatchAsync(conn, tx, pending, cancellationToken).ConfigureAwait(false);
                            pending.Clear();
                        }

                        if (delayMs > 0 && i < chunkTexts.Count - 1)
                            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                    }

                    if (pending.Count > 0)
                        await InsertBatchAsync(conn, tx, pending, cancellationToken).ConfigureAwait(false);

                    await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    throw;
                }

                summary.FilesProcessed++;
            }

            return summary;
        }

        private readonly struct ChunkRow
        {
            public ChunkRow(string sourceFile, int chunkIndex, string content, float[] embedding, string metadataJson)
            {
                SourceFile = sourceFile;
                ChunkIndex = chunkIndex;
                Content = content;
                Embedding = embedding;
                MetadataJson = metadataJson;
            }

            public string SourceFile { get; }
            public int ChunkIndex { get; }
            public string Content { get; }
            public float[] Embedding { get; }
            public string MetadataJson { get; }
        }

        private static async Task DeleteChunksForSourceInTransactionAsync(
            NpgsqlConnection conn,
            NpgsqlTransaction tx,
            string sourceFile,
            CancellationToken cancellationToken)
        {
            await using var cmd = new NpgsqlCommand(
                "DELETE FROM manual_chunks WHERE source_file = @s",
                conn,
                tx);
            cmd.Parameters.AddWithValue("s", sourceFile);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task InsertBatchAsync(
            NpgsqlConnection conn,
            NpgsqlTransaction tx,
            List<ChunkRow> rows,
            CancellationToken cancellationToken)
        {
            if (rows == null || rows.Count == 0)
                return;

            var sb = new StringBuilder();
            sb.AppendLine(
                "INSERT INTO manual_chunks (content, source_file, chunk_index, embedding, metadata) VALUES ");
            var parameters = new List<NpgsqlParameter>();
            for (var i = 0; i < rows.Count; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append($"(@c{i},@s{i},@n{i},@e{i},@m{i}::jsonb)");
                var r = rows[i];
                parameters.Add(new NpgsqlParameter($"c{i}", r.Content));
                parameters.Add(new NpgsqlParameter($"s{i}", r.SourceFile));
                parameters.Add(new NpgsqlParameter($"n{i}", r.ChunkIndex));
                parameters.Add(NpgsqlVectorParameter.Create($"e{i}", new Vector(r.Embedding)));
                parameters.Add(new NpgsqlParameter($"m{i}", r.MetadataJson));
            }

            sb.AppendLine();
            sb.AppendLine(
                "ON CONFLICT (source_file, chunk_index) DO UPDATE SET content = EXCLUDED.content, embedding = EXCLUDED.embedding, metadata = EXCLUDED.metadata");

            await using var cmd = new NpgsqlCommand(sb.ToString(), conn, tx);
            cmd.Parameters.AddRange(parameters.ToArray());
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
