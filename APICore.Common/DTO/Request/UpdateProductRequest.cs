#nullable enable

using System.Collections.Generic;

namespace APICore.Common.DTO.Request
{
    public class UpdateProductRequest
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
        public decimal? Precio { get; set; }
        public decimal? Costo { get; set; }
        public string? ImagenUrl { get; set; }
        public bool? IsAvailable { get; set; }
        public bool? IsForSale { get; set; }
        public string? Tipo { get; set; }

        public int? StockParentProductId { get; set; }
        public decimal? StockUnitsConsumedPerSaleUnit { get; set; }
        /// <summary>Unidades de venta por cada 1 unidad de stock del padre (ej. 55 si 1 saco = 55 lb). Si se envía, el backend recalcula StockUnitsConsumedPerSaleUnit = 1 / este valor.</summary>
        public decimal? SaleUnitsPerParentStockUnit { get; set; }
        /// <summary>Si es true, quita el vínculo de stock padre (y el factor).</summary>
        public bool? ClearStockParentLink { get; set; }

        /// <summary>Si no es null, reemplaza las ofertas por tienda para <c>elaborado</c>. Ignorado para inventariable (se limpian ofertas al pasar a inventariable).</summary>
        public List<int>? OfferLocationIds { get; set; }
        public List<int>? TagIds { get; set; }
    }
}
