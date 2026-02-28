using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class ChangeAccountStatusRequest
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public bool Active { get; set; }
    }
}