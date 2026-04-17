using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class PaymentMethod : BaseEntity
    {
        [Required]
        public int OrganizationId { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public Organization? Organization { get; set; }
        public ICollection<SaleOrderPayment> SaleOrderPayments { get; set; } = new List<SaleOrderPayment>();
    }
}
