using System;

namespace APICore.Data.Entities
{
    public class Lead : BaseEntity
    {
        public int OrganizationId { get; set; }
        public string Name { get; set; } = null!;
        public string? Company { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Origin { get; set; }
        public string Status { get; set; } = "Nuevo";
        public string? Notes { get; set; }
        public int? AssignedUserId { get; set; }
        public int? ConvertedToContactId { get; set; }
        public DateTime? ConvertedAt { get; set; }

        public virtual Organization? Organization { get; set; }
        public virtual User? AssignedUser { get; set; }
        public virtual Contact? ConvertedToContact { get; set; }
    }
}
