using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class CreatePaymentMethodRequest
    {
        [Required]
        [MaxLength(120)]
        public string Name { get; set; } = null!;

        public int SortOrder { get; set; }
    }
}
