using System.Collections.Generic;

namespace APICore.Data.Entities
{
    /// <summary>
    /// Moneda por organización. CUP es la moneda base por org (tipo de cambio 1, no editable).
    /// </summary>
    public class Currency : BaseEntity
    {
        public int OrganizationId { get; set; }
        public Organization Organization { get; set; } = null!;

        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        /// <summary>Tipo de cambio respecto al CUP de esta organización.</summary>
        public decimal ExchangeRate { get; set; }
        public bool IsActive { get; set; } = true;
        /// <summary>Solo CUP por organización. No editable desde la API.</summary>
        public bool IsBase { get; set; }

        public ICollection<CurrencyDenomination> Denominations { get; set; } = new List<CurrencyDenomination>();
    }
}
