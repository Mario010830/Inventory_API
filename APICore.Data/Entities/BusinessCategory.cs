using System.Collections.Generic;

namespace APICore.Data.Entities
{
    /// <summary>
    /// Categoría de negocio global (catálogo para clasificar locales).
    /// </summary>
    public class BusinessCategory : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Icon { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }

        public virtual ICollection<Location> Locations { get; set; } = new List<Location>();
    }
}
