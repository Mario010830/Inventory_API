using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class CancelSubscriptionRequestDto
    {
        [Required]
        public string Notes { get; set; } = null!;
    }
}
