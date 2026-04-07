using System;

namespace APICore.Data.Entities
{
    /// <summary>
    /// Representa una fila de <c>manual_chunks</c>. La tabla se gestiona vía SQL/pgvector y Npgsql;
    /// no está registrada en <see cref="CoreDbContext"/> para no mezclar tipos vector con migraciones EF estándar.
    /// </summary>
    public class ManualChunk
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string SourceFile { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public string? Metadata { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
