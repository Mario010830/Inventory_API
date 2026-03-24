namespace APICore.Common.DTO.Request
{
    public class UpdateBusinessCategoryRequest
    {
        public string? Name { get; set; }
        public string? Icon { get; set; }
        public bool? IsActive { get; set; }
        public int? SortOrder { get; set; }
    }
}
