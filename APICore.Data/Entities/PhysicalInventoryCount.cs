using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    /// <summary>Conteo físico de cierre vinculado a un <see cref="DailySummary"/> (cuadre diario / cierre de caja).</summary>
    public class PhysicalInventoryCount : BaseEntity
    {
        [Required]
        public int DailySummaryId { get; set; }

        /// <summary>Momento en que se generó o actualizó el conteo (UTC).</summary>
        [Required]
        public DateTime CountedAt { get; set; }

        public int? UserId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = PhysicalInventoryCountStatus.Draft;

        public DailySummary? DailySummary { get; set; }
        public User? User { get; set; }

        public ICollection<PhysicalInventoryCountItem> Items { get; set; } = new List<PhysicalInventoryCountItem>();
    }
}
