using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class UpdatePaymentMethodRequest
    {
        [MaxLength(120)]
        public string? Name { get; set; }

        public int? SortOrder { get; set; }

        public bool? IsActive { get; set; }
    }
}
