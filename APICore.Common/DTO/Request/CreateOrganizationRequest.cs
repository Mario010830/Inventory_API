namespace APICore.Common.DTO.Request
{
    public class CreateOrganizationRequest
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }
    }
}
