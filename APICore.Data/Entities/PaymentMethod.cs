using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class PaymentMethod : BaseEntity
    {
        [Required]
        public int OrganizationId { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Identificador de la cuenta/tarjeta predefinida (p. ej. últimos 4, CLABE enmascarada).
        /// Permite varias filas con el mismo <see cref="Name"/> si difiere la referencia. No almacenar PAN completo.
        /// </summary>
        [MaxLength(120)]
        public string? InstrumentReference { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public Organization? Organization { get; set; }
        public ICollection<SaleOrderPayment> SaleOrderPayments { get; set; } = new List<SaleOrderPayment>();
    }
}
