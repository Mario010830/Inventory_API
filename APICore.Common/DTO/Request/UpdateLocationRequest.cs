namespace APICore.Common.DTO.Request
{
    public class UpdateLocationRequest
    {
        public int? OrganizationId { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        /// <summary>Teléfono WhatsApp (código país sin +). Ej: 5215512345678.</summary>
        public string? WhatsAppContact { get; set; }
    }
}
