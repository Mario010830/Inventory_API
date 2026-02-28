#nullable enable

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
    }
}
