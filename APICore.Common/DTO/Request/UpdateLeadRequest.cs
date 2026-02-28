#nullable enable

namespace APICore.Common.DTO.Request
{
    public class UpdateLeadRequest
    {
        public string? Name { get; set; }
        public string? Company { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Origin { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public int? AssignedUserId { get; set; }
    }
}
