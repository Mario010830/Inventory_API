using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class DailySummaryInventoryItem : BaseEntity
    {
        [Required]
        public int DailySummaryId { get; set; }

        [Required]
        public int ProductId { get; set; }

        /// <summary>Nombre del producto desnormalizado al momento del cuadre.</summary>
        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        public decimal QuantitySold { get; set; }

        public decimal StockBefore { get; set; }

        public decimal StockAfter { get; set; }

        /// <summary>Calculado: StockBefore - StockAfter - QuantitySold. Debe ser 0 si no hay discrepancia.</summary>
        public decimal StockDifference { get; set; }

        public DailySummary? DailySummary { get; set; }
        public Product? Product { get; set; }
    }
}
