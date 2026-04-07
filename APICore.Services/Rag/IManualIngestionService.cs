using System.Threading;
using System.Threading.Tasks;

namespace APICore.Services.Rag
{
    public interface IManualIngestionService
    {
        /// <summary>Lee <c>Rag:ManualPath</c>, fragmenta archivos .md/.txt, genera embeddings e inserta en <c>manual_chunks</c>.</summary>
        Task<ManualIngestionSummary> IngestManualAsync(CancellationToken cancellationToken = default);
    }

    public sealed class ManualIngestionSummary
    {
        public int FilesProcessed { get; set; }
        public int ChunksWritten { get; set; }
    }
}
