using System;

namespace APICore.Common.DTO.Response
{
    public class LocationResponse
    {
        public int Id { get; set; }
        public int OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }
        public string? WhatsAppContact { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Province { get; set; }
        public string? Municipality { get; set; }
        public string? Street { get; set; }
        /// <summary>Horario de atención (catálogo público). null si no está configurado.</summary>
        public PublicLocationBusinessHoursDto? BusinessHours { get; set; }
        /// <summary>Coordenadas para mapa (catálogo público). null si no están configuradas.</summary>
        public PublicLocationCoordinatesDto? Coordinates { get; set; }
        /// <summary>Si el local está abierto "ahora mismo" según horario y hora del servidor.</summary>
        public bool IsOpenNow { get; set; }
        public bool IsVerified { get; set; }
        /// <summary>La ubicación ofrece domicilio (configuración en BD). Combinar con <see cref="IsOpenNow"/> para saber si aplica en este momento.</summary>
        public bool OffersDelivery { get; set; }
        /// <summary>La ubicación ofrece recogida en tienda (configuración en BD). Combinar con <see cref="IsOpenNow"/> para saber si aplica en este momento.</summary>
        public bool OffersPickup { get; set; }
        public int? BusinessCategoryId { get; set; }
        public BusinessCategorySummaryDto? BusinessCategory { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}
