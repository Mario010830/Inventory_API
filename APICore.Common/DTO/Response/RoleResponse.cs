using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    public class RoleResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public List<int> PermissionIds { get; set; } = new List<int>();
    }
}
