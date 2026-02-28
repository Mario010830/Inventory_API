using System;

namespace APICore.Common.DTO.Response
{
    public class LeadResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Company { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Origin { get; set; }
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
        public int? AssignedUserId { get; set; }
        public int? ConvertedToContactId { get; set; }
        public DateTime? ConvertedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}
