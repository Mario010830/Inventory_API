using System;
using System.Collections.Generic;

namespace APICore.Data.Entities
{
    public class Contact : BaseEntity
    {
        public int OrganizationId { get; set; }
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

        /// <summary>Rol cliente: puede asociarse a ventas.</summary>
        public bool IsCustomer { get; set; } = true;

        /// <summary>Rol proveedor: entradas de inventario pueden referenciar este contacto.</summary>
        public bool IsSupplier { get; set; }

        /// <summary>Pipeline tipo lead; null si no es lead activo.</summary>
        public string? LeadStatus { get; set; }

        /// <summary>Fecha en que el lead se consideró convertido (cliente).</summary>
        public DateTime? LeadConvertedAt { get; set; }

        public virtual Organization? Organization { get; set; }
        public virtual User? AssignedUser { get; set; }
    }
}
