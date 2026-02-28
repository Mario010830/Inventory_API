using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#nullable enable

namespace APICore.Common.DTO.Request
{
    public class UpdateUserRequest
    {
        public string? FullName { get; set; }
        public string? OldPassword { get; set; }
        public string? Password { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        
        public int? LocationId { get; set; }
        public int? OrganizationId { get; set; }

        public int? RoleId { get; set; }
    }
}
