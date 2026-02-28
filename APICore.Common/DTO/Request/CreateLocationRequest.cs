namespace APICore.Common.DTO.Request
{
    public class CreateLocationRequest
    {
        public int OrganizationId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }
    }
}
