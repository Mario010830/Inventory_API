using System.IO;
using System.Threading.Tasks;

namespace APICore.Services
{
    /// <summary>
    /// Servicio para almacenar archivos (imágenes de productos) en S3 u otro proveedor.
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Sube una imagen de producto y devuelve la URL pública.
        /// </summary>
        /// <param name="fileStream">Stream del archivo.</param>
        /// <param name="fileName">Nombre original del archivo (para la extensión).</param>
        /// <param name="contentType">Tipo MIME (ej: image/jpeg).</param>
        /// <returns>URL pública del archivo subido.</returns>
        Task<string> UploadProductImageAsync(Stream fileStream, string fileName, string contentType);

        /// <summary>
        /// Elimina una imagen del almacenamiento por su clave/URL.
        /// </summary>
        Task DeleteProductImageAsync(string objectKeyOrUrl);
    }
}
