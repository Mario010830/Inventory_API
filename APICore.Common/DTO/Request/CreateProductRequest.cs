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
        /// <summary>Ids de ubicaciones donde se ofrece el producto (solo aplica a <c>elaborado</c>).</summary>
        public List<int>? OfferLocationIds { get; set; }
        public List<int>? TagIds { get; set; }
    }
}
