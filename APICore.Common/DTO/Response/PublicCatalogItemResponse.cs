using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    /// <summary>Imagen del catálogo público (orden y principal para galería).</summary>
    public class PublicCatalogImageItem
    {
        public string ImageUrl { get; set; } = null!;
        public int SortOrder { get; set; }
        public bool IsMain { get; set; }
    }

    /// <summary>
    /// Respuesta pública del catálogo de ventas. No expone el costo (dato interno).
    /// </summary>
    public class PublicCatalogItemResponse
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        /// <summary>URL de la imagen principal (misma lógica que el backoffice).</summary>
        public string? ImagenUrl { get; set; }
        /// <summary>Todas las imágenes del producto, ordenadas por <see cref="PublicCatalogImageItem.SortOrder"/>.</summary>
        public List<PublicCatalogImageItem> Images { get; set; } = new List<PublicCatalogImageItem>();
        public decimal Precio { get; set; }
        public decimal OriginalPrecio { get; set; }
        public bool HasActivePromotion { get; set; }
        public string? PromotionType { get; set; }
        public decimal? PromotionValue { get; set; }
        public int? PromotionId { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryColor { get; set; }
        public string Tipo { get; set; } = "inventariable";
        public decimal StockAtLocation { get; set; }
        /// <summary>
        /// Indica si el local donde se muestra este producto está abierto "ahora mismo".
        /// Todos los ítems del mismo catálogo comparten el mismo valor.
        /// </summary>
        public bool? IsOpenNow { get; set; }
        /// <summary>
        /// Id de la ubicación a la que pertenece este producto en el catálogo público (solo para all=true).
        /// </summary>
        public int? LocationId { get; set; }
        /// <summary>
        /// Nombre de la ubicación a la que pertenece este producto en el catálogo público (solo para all=true).
        /// </summary>
        public string? LocationName { get; set; }
        /// <summary>Etiquetas del producto (id, name, slug, color). Array vacío si no tiene.</summary>
        public List<TagDto> Tags { get; set; } = new List<TagDto>();
    }

    public class PublicCatalogPaginatedResponse
    {
        public IEnumerable<PublicCatalogItemResponse> Items { get; set; } = System.Linq.Enumerable.Empty<PublicCatalogItemResponse>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
    }
}
