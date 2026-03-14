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
        /// incluyendo el stock disponible en esa ubicación.
        /// No requiere autenticación.
        /// </summary>
        Task<IEnumerable<PublicCatalogItemResponse>> GetCatalogByLocationAsync(int locationId);
    }
}
