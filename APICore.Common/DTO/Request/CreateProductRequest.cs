using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Request
{
    public class CreateProductRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? CategoryId { get; set; }
        public decimal Precio { get; set; }
        public decimal Costo { get; set; }
        public string ImagenUrl { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsForSale { get; set; }
        public string Tipo { get; set; } = "inventariable";

        /// <summary>Producto cuyo inventario se descuenta (ej. saco). Requiere factor en StockUnitsConsumedPerSaleUnit.</summary>
        public int? StockParentProductId { get; set; }

        /// <summary>Unidades de stock del padre consumidas por cada 1 unidad vendida de este producto (ej. 1/55 si el padre es saco de 55 lb y aquí se vende por lb).</summary>
        public decimal? StockUnitsConsumedPerSaleUnit { get; set; }

        /// <summary>Unidades de venta por cada 1 unidad de stock del padre (ej. 55 si 1 saco = 55 lb). Si se envía, el backend calcula StockUnitsConsumedPerSaleUnit = 1 / este valor.</summary>
        public decimal? SaleUnitsPerParentStockUnit { get; set; }

        /// <summary>Ids de ubicaciones donde se ofrece el producto (solo aplica a <c>elaborado</c>).</summary>
        public List<int>? OfferLocationIds { get; set; }
        public List<int>? TagIds { get; set; }
    }
}
