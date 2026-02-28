using System.Collections.Generic;

namespace APICore.Common.DTO.Request
{
    public class UpdateRoleRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<int>? PermissionIds { get; set; }
    }
}
