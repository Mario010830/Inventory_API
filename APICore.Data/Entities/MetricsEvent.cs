using System;
using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    /// <summary>
    /// Evento de analítica del catálogo público (una fila por ocurrencia).
    /// </summary>
    public class MetricsEvent : BaseEntity
    {
        public int OrganizationId { get; set; }

        /// <summary>Ubicación del catálogo (opcional según tipo de evento).</summary>
        public int? LocationId { get; set; }

        [Required]
        [MaxLength(64)]
        public string EventType { get; set; } = string.Empty;

        public DateTime OccurredAt { get; set; }

        public int? UserId { get; set; }

        [MaxLength(128)]
        public string? SessionId { get; set; }

        public int? ProductId { get; set; }

        public int? SaleOrderId { get; set; }

        [MaxLength(32)]
        public string? TrafficSource { get; set; }

        [MaxLength(512)]
        public string? SearchTerm { get; set; }

        public int? DurationSeconds { get; set; }

        public Organization? Organization { get; set; }
        public Location? Location { get; set; }
        public Product? Product { get; set; }
        public User? User { get; set; }
        public SaleOrder? SaleOrder { get; set; }
    }
}
