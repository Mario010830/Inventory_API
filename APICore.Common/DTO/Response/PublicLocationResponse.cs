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
    }
}
