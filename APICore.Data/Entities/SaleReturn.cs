using APICore.Data.Entities.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class SaleReturn : BaseEntity
    {
        [Required]
        public int OrganizationId { get; set; }

        [Required]
        public int SaleOrderId { get; set; }

        [Required]
        public int LocationId { get; set; }

        public SaleReturnStatus Status { get; set; } = SaleReturnStatus.pending;

        public string? Reason { get; set; }
        public string? Notes { get; set; }

        public decimal Total { get; set; }

        public int? UserId { get; set; }

        public SaleOrder? SaleOrder { get; set; }
        public Location? Location { get; set; }
        public Organization? Organization { get; set; }

        public ICollection<SaleReturnItem> Items { get; set; } = new List<SaleReturnItem>();
    }
}
