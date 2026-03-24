using APICore.Common.DTO.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface IPublicCatalogService
    {
        /// <summary>
        /// Lista todas las ubicaciones/negocios disponibles para que el usuario elija.
        /// No requiere autenticación.
        /// </summary>
        Task<IEnumerable<PublicLocationResponse>> GetLocationsAsync();

        /// <summary>
        /// Devuelve el catálogo de productos con IsForSale = true para una ubicación específica,
        /// incluyendo el stock disponible en esa ubicación y todas las imágenes públicas (Images).
        /// No requiere autenticación.
        /// </summary>
        Task<IEnumerable<PublicCatalogItemResponse>> GetCatalogByLocationAsync(int locationId);

        /// <summary>
        /// Devuelve el catálogo público combinado de todas las ubicaciones activas,
        /// con paginación.
        /// No requiere autenticación.
        /// </summary>
        Task<PublicCatalogPaginatedResponse> GetCatalogAllAsync(int page, int pageSize);

        /// <summary>
        /// Lista etiquetas que tienen al menos un producto público (IsForSale) asignado.
        /// No requiere autenticación. Para filtros en el catálogo.
        /// </summary>
        Task<IEnumerable<TagDto>> GetPublicTagsAsync();
    }
}
