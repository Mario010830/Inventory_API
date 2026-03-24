using System;

namespace APICore.Common.DTO.Response
{
    public class BusinessCategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Icon { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Resumen para anidar en Location (nombre + icono).</summary>
    public class BusinessCategorySummaryDto
    {
        public string Name { get; set; } = null!;
        public string? Icon { get; set; }
    }
}
