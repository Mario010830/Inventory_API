namespace APICore.Common.DTO.Request
{
    public class CreateProductCategoryRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public string Color { get; set; } = "#6366f1";
        public string Icon { get; set; } = "category";
    }
}
