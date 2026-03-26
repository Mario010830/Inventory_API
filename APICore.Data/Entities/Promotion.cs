using APICore.Data.Entities.Enums;
using System;

namespace APICore.Data.Entities
{
    public class Promotion : BaseEntity
    {
        public int OrganizationId { get; set; }
        public int ProductId { get; set; }
        public PromotionType Type { get; set; } = PromotionType.percentage;
        public decimal Value { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public bool IsActive { get; set; } = true;
        public int MinQuantity { get; set; } = 1;

        public Organization? Organization { get; set; }
        public Product? Product { get; set; }
    }
}
