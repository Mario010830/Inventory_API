using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Common.DTO.Request
{
    public class CatalogMetricsBatchRequest
    {
        [Required]
        public int LocationId { get; set; }

        [MaxLength(128)]
        public string? SessionId { get; set; }

        [Required]
        [MinLength(1)]
        public IList<CatalogMetricEventItemRequest> Events { get; set; } = new List<CatalogMetricEventItemRequest>();
    }

    public class CatalogMetricEventItemRequest
    {
        [Required]
        [MaxLength(64)]
        public string Type { get; set; } = string.Empty;

        public DateTime? OccurredAt { get; set; }

        /// <summary>Catálogo (ubicación); por defecto usa <see cref="CatalogMetricsBatchRequest.LocationId"/>.</summary>
        public int? CatalogId { get; set; }

        public int? ProductId { get; set; }

        [MaxLength(32)]
        public string? TrafficSource { get; set; }

        [MaxLength(512)]
        public string? SearchTerm { get; set; }

        public int? DurationSeconds { get; set; }
    }
}
