using System;

namespace APICore.Common.DTO.Response
{
    public class ProductImageResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; }
        public int SortOrder { get; set; }
        public bool IsMain { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
