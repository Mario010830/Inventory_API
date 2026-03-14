namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// Respuesta pública del catálogo de ventas. No expone el costo (dato interno).
    /// </summary>
    public class PublicCatalogItemResponse
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImagenUrl { get; set; }
        public decimal Precio { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategoryColor { get; set; }
        public decimal StockAtLocation { get; set; }
    }
}
