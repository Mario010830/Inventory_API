namespace APICore.Data.Entities
{
    public class ProductImage : BaseEntity
    {
        public int ProductId { get; set; }
        public string ImageUrl { get; set; }
        public int SortOrder { get; set; }
        public bool IsMain { get; set; }

        public Product? Product { get; set; }
    }
}
