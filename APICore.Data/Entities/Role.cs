using System.Collections.Generic;

namespace APICore.Data.Entities
{

    public class Role : BaseEntity
    {
        /// <summary>Null = system role (visible to all orgs), otherwise scoped to that organization.</summary>
        public int? OrganizationId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
        public Organization? Organization { get; set; }
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
