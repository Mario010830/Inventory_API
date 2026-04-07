using System.Threading;
using System.Threading.Tasks;

namespace APICore.Services.Rag
{
    public interface IEmbeddingService
    {
        /// <summary>Vector de embedding; longitud debe coincidir con <c>Rag:EmbeddingDimension</c>.</summary>
        Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
    }
}
