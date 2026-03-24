using System;

namespace APICore.Common.DTO.Response
{
    public class TagResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Color { get; set; }
        public int ProductCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Etiqueta resumida para incluir en producto (admin y catálogo público).
    /// </summary>
    public class TagDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Color { get; set; }
    }
}
