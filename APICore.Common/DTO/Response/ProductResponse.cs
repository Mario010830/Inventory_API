using System;

namespace APICore.Common.DTO.Response
{
    public class ProductResponse
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public ProductCategoryResponse? Category { get; set; }
        public decimal Precio { get; set; }
        public decimal Costo { get; set; }
        public string ImagenUrl { get; set; }
        public bool IsAvailable { get; set; }
        public decimal TotalStock { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}
