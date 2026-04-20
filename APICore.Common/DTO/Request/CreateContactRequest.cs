namespace APICore.Common.DTO.Request
{
    public class CreateContactRequest
    {
        public string Name { get; set; } = null!;
        public string? Company { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public string? Origin { get; set; }
        public bool IsActive { get; set; } = true;
        public int? AssignedUserId { get; set; }

        /// <summary>Si no se envía en creación simple, se asume true (cliente).</summary>
        public bool? IsCustomer { get; set; }

        public bool? IsSupplier { get; set; }

        /// <summary>Estado de pipeline lead; null si no aplica.</summary>
        public string? LeadStatus { get; set; }
    }
}
