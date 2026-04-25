using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class PhysicalInventoryCountItem : BaseEntity
    {
        [Required]
        public int PhysicalInventoryCountId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>Cantidad esperada según sistema (fórmula: stock actual + vendido en periodo − devuelto en periodo).</summary>
        public decimal ExpectedQuantity { get; set; }

        /// <summary>Cantidad contada físicamente; null hasta que el usuario guarde.</summary>
        public decimal? CountedQuantity { get; set; }

        /// <summary>Contada − esperada (positivo = sobrante físico).</summary>
        public decimal Difference { get; set; }

        /// <summary>Precio unitario de referencia al generar el conteo (para valorizar diferencias).</summary>
        public decimal UnitPrice { get; set; }

        /// <summary><see cref="Difference"/> × <see cref="UnitPrice"/>.</summary>
        public decimal ValuedDifference { get; set; }

        public PhysicalInventoryCount? PhysicalInventoryCount { get; set; }
        public Product? Product { get; set; }
    }
}
