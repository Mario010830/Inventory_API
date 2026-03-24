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
        public List<int>? TagIds { get; set; }
    }
}
