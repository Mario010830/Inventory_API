using System.Collections.Generic;

namespace APICore.Data.Entities
{
    public class Contact : BaseEntity
    {
        public int OrganizationId { get; set; }
        public string Name { get; set; } = null!;
        public string? Company { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public string? Origin { get; set; }
        public bool IsActive { get; set; } = true;
        public int? AssignedUserId { get; set; }

        public virtual Organization? Organization { get; set; }
        public virtual User? AssignedUser { get; set; }
    }
}
