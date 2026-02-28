using System.Collections.Generic;

namespace APICore.Data.Entities
{

    public class Permission : BaseEntity
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }

        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
