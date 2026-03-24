namespace APICore.Common.DTO.Request
{
    public class CreateBusinessCategoryRequest
    {
        public string Name { get; set; } = null!;
        public string? Icon { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }
}
