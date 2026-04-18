using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    /// <summary>Valor facial de billete/moneda para una moneda de la organización.</summary>
    public class CurrencyDenomination : BaseEntity
    {
        [Required]
        public int CurrencyId { get; set; }

        public Currency Currency { get; set; } = null!;

        /// <summary>Valor facial (ej. 100, 50, 3).</summary>
        public decimal Value { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
