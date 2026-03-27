using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    public class ProductResponse
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? CategoryId { get; set; }
        public ProductCategoryResponse? Category { get; set; }
        public decimal Precio { get; set; }
        public decimal Costo { get; set; }
        public string ImagenUrl { get; set; }
        public List<ProductImageResponse> ProductImages { get; set; } = new List<ProductImageResponse>();
        public bool IsAvailable { get; set; }
        public bool IsForSale { get; set; }
        public string Tipo { get; set; } = "inventariable";
        /// <summary>Ubicaciones donde el producto elaborado está ofertado (vacío si no aplica o sin filas).</summary>
        public List<int> OfferLocationIds { get; set; } = new List<int>();
        public decimal TotalStock { get; set; }
        public List<TagDto> Tags { get; set; } = new List<TagDto>();
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}
