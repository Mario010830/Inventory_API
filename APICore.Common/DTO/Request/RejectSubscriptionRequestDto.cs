using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class RejectSubscriptionRequestDto
    {
        [Required]
        public string Notes { get; set; }
    }
}
