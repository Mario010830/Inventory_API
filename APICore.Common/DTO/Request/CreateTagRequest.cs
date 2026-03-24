namespace APICore.Common.DTO.Request
{
    public class CreateTagRequest
    {
        public string Name { get; set; } = null!;
        public string? Color { get; set; }
    }
}
