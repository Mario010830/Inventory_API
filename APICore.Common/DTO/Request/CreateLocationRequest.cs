namespace APICore.Common.DTO.Request
{
    public class CreateLocationRequest
    {
        public int OrganizationId { get; set; }
        /// <summary>Categoría de negocio opcional (catálogo global).</summary>
        public int? BusinessCategoryId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string? Description { get; set; }
        /// <summary>Teléfono WhatsApp (código país sin +). Ej: 5215512345678. Para enlace "Enviar pedido por WhatsApp".</summary>
        public string? WhatsAppContact { get; set; }
        /// <summary>URL de la foto (obtener subiendo con POST /api/location/image).</summary>
        public string? PhotoUrl { get; set; }
        public string? Province { get; set; }
        public string? Municipality { get; set; }
        public string? Street { get; set; }
        /// <summary>Horario de atención opcional por día. null = sin horario configurado.</summary>
        public PublicLocationBusinessHoursRequest? BusinessHours { get; set; }
        /// <summary>Coordenadas opcionales para mapa. null = sin mapa.</summary>
        public PublicLocationCoordinatesRequest? Coordinates { get; set; }
        public bool IsVerified { get; set; }
        public bool OffersDelivery { get; set; } = true;
        public bool OffersPickup { get; set; } = true;
    }

    public class PublicLocationBusinessHoursRequest
    {
        public PublicLocationDayHoursRequest? Monday { get; set; }
        public PublicLocationDayHoursRequest? Tuesday { get; set; }
        public PublicLocationDayHoursRequest? Wednesday { get; set; }
        public PublicLocationDayHoursRequest? Thursday { get; set; }
        public PublicLocationDayHoursRequest? Friday { get; set; }
        public PublicLocationDayHoursRequest? Saturday { get; set; }
        public PublicLocationDayHoursRequest? Sunday { get; set; }
    }

    public class PublicLocationDayHoursRequest
    {
        /// <summary>Hora de apertura en formato HH:mm (UTC o zona horaria acordada).</summary>
        public string Open { get; set; }
        /// <summary>Hora de cierre en formato HH:mm (UTC o zona horaria acordada).</summary>
        public string Close { get; set; }
    }

    public class PublicLocationCoordinatesRequest
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
}
