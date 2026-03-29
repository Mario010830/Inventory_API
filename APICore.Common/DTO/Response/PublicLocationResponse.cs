using System;

namespace APICore.Common.DTO.Response
{
    public class PublicLocationResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int OrganizationId { get; set; }
        public string OrganizationName { get; set; } = null!;
        /// <summary>Teléfono para WhatsApp (con código país, sin +). Ej: 5215512345678. El front arma el link wa.me/{whatsappContact}.</summary>
        public string? WhatsAppContact { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Province { get; set; }
        public string? Municipality { get; set; }
        public string? Street { get; set; }
        /// <summary>Horario de atención público por día. null = sin horario configurado.</summary>
        public PublicLocationBusinessHoursDto? BusinessHours { get; set; }
        /// <summary>Coordenadas para mapa público. null = sin coordenadas.</summary>
        public PublicLocationCoordinatesDto? Coordinates { get; set; }
        /// <summary>Indica si el local está abierto "ahora mismo" según la hora UTC del servidor.</summary>
        public bool IsOpenNow { get; set; }
        public bool IsVerified { get; set; }
        /// <summary>Disponible para domicilio; en respuestas API es falso si la tienda está cerrada ahora (<see cref="IsOpenNow"/>).</summary>
        public bool OffersDelivery { get; set; }
        /// <summary>Disponible para recogida; en respuestas API es falso si la tienda está cerrada ahora (<see cref="IsOpenNow"/>).</summary>
        public bool OffersPickup { get; set; }
        public DateTime CreatedAt { get; set; }
        /// <summary>Cantidad de productos disponibles en esta ubicación.</summary>
        public int ProductCount { get; set; }
        /// <summary>Indica si al menos un producto tiene promoción activa.</summary>
        public bool HasPromo { get; set; }
        public int? BusinessCategoryId { get; set; }
        public BusinessCategorySummaryDto? BusinessCategory { get; set; }
    }

    public class PublicLocationBusinessHoursDto
    {
        public PublicLocationDayHoursDto? Monday { get; set; }
        public PublicLocationDayHoursDto? Tuesday { get; set; }
        public PublicLocationDayHoursDto? Wednesday { get; set; }
        public PublicLocationDayHoursDto? Thursday { get; set; }
        public PublicLocationDayHoursDto? Friday { get; set; }
        public PublicLocationDayHoursDto? Saturday { get; set; }
        public PublicLocationDayHoursDto? Sunday { get; set; }
    }

    public class PublicLocationDayHoursDto
    {
        /// <summary>Hora de apertura en formato HH:mm (UTC o zona horaria acordada).</summary>
        public string Open { get; set; } = null!;
        /// <summary>Hora de cierre en formato HH:mm (UTC o zona horaria acordada).</summary>
        public string Close { get; set; } = null!;
    }

    public class PublicLocationCoordinatesDto
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}
