using System;
using System.Collections.Generic;

namespace APICore.Data.Entities
{
    /// <summary>
    /// Etiqueta global creada por el admin. Los negocios la asignan a productos.
    /// </summary>
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Color { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();
    }
}
