using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APICore.Common.DTO.Request
{
    public class CreateUserRequest
    {
        public string FullName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime BirthDate { get; set; }
        public int? LocationId { get; set; }
        public int? RoleId { get; set; }
    }
}
