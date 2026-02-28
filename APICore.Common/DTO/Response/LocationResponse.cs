using System;

namespace APICore.Common.DTO.Response
{
    public class LocationResponse
    {
        public int Id { get; set; }
        public int OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}
