using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class CreateOrUpdatePlanRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string DisplayName { get; set; }
        public string? Description { get; set; }
        public int MaxProducts { get; set; }
        public int MaxUsers { get; set; }
        public int MaxLocations { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal AnnualPrice { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
