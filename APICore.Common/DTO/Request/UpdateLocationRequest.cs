namespace APICore.Common.DTO.Request
{
    public class UpdateLocationRequest
    {
        public int? OrganizationId { get; set; }
        /// <summary>Id de categoría de negocio. null = no cambiar. Valor menor o igual a 0 = quitar categoría.</summary>
        public int? BusinessCategoryId { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        /// <summary>Teléfono WhatsApp (código país sin +). Ej: 5215512345678.</summary>
        public string? WhatsAppContact { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Province { get; set; }
        public string? Municipality { get; set; }
        public string? Street { get; set; }
        /// <summary>Horario de atención opcional por día. null = no cambiar / limpiar según convención.</summary>
        public PublicLocationBusinessHoursRequest? BusinessHours { get; set; }
        /// <summary>Coordenadas opcionales. null = no cambiar / limpiar según convención.</summary>
        public PublicLocationCoordinatesRequest? Coordinates { get; set; }
        public bool? IsVerified { get; set; }
        public bool? OffersDelivery { get; set; }
        public bool? OffersPickup { get; set; }
        /// <summary>Horario de domicilio. null = no cambiar.</summary>
        public PublicLocationBusinessHoursRequest? DeliveryHours { get; set; }
        /// <summary>Horario de recogida. null = no cambiar.</summary>
        public PublicLocationBusinessHoursRequest? PickupHours { get; set; }
    }
}
