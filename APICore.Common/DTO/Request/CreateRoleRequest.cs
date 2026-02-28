using System.Collections.Generic;

namespace APICore.Common.DTO.Request
{
    public class CreateRoleRequest
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        /// <summary>
        /// Ids de los permisos a asignar al rol.
        /// </summary>
        public List<int> PermissionIds { get; set; } = new List<int>();
    }
}
