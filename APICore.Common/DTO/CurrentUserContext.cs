using System.Collections.Generic;

namespace APICore.Common.DTO
{

    public class CurrentUserContext
    {
        public int UserId { get; set; }
        public int? LocationId { get; set; }
       
        public int? OrganizationId { get; set; }
        public int? RoleId { get; set; }

        public bool IsSuperAdmin { get; set; }

        public bool IsAdmin { get; set; }
        public IReadOnlyList<string> PermissionCodes { get; set; } = new List<string>();
    }
}
