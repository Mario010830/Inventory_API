using System;

namespace APICore.Data.Entities
{
    public class LoyaltyEvent : BaseEntity
    {
        public int OrganizationId { get; set; }
        public int ContactId { get; set; }
        public int? SaleOrderId { get; set; }
        public int PointsDelta { get; set; }
        public DateTime OccurredAt { get; set; }
        /// <summary>Ej. umbral de compras alcanzado (avisos).</summary>
        public string? Note { get; set; }

        public virtual Organization Organization { get; set; } = null!;
        public virtual Contact Contact { get; set; } = null!;
        public virtual SaleOrder? SaleOrder { get; set; }
    }
}
