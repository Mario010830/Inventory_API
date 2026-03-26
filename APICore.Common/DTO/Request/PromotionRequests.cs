using System;
using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class CreatePromotionRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [RegularExpression("^(percentage|fixed)$")]
        public string Type { get; set; } = "percentage";

        [Range(0.0000001, double.MaxValue)]
        public decimal Value { get; set; }

        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public bool IsActive { get; set; } = true;

        [Range(1, int.MaxValue)]
        public int MinQuantity { get; set; } = 1;
    }

    public class UpdatePromotionRequest
    {
        [RegularExpression("^(percentage|fixed)$")]
        public string? Type { get; set; }
        public decimal? Value { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public bool? IsActive { get; set; }
        public int? MinQuantity { get; set; }
    }
}
