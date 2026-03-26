using System;

namespace APICore.Common.DTO.Response
{
    public class PromotionResponse
    {
        public int Id { get; set; }
        public int OrganizationId { get; set; }
        public int ProductId { get; set; }
        public string PromotionType { get; set; } = null!;
        public decimal Value { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public bool IsActive { get; set; }
        public int MinQuantity { get; set; }
        public bool IsCurrentlyValid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}
