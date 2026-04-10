using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class DailySummary : BaseEntity
    {
        /// <summary>Fecha del cuadre (solo fecha, sin hora). Usar Date.Date al asignar.</summary>
        [Required]
        public DateTime Date { get; set; }

        [Required]
        public int LocationId { get; set; }

        [Required]
        public int OrganizationId { get; set; }

        /// <summary>Fondo inicial de caja.</summary>
        public decimal OpeningCash { get; set; }

        /// <summary>Suma de SaleOrder.Total con Status = confirmed del día.</summary>
        public decimal TotalSales { get; set; }

        /// <summary>Suma de SaleReturn.Total del día.</summary>
        public decimal TotalReturns { get; set; }

        /// <summary>Salidas de caja manuales (por ahora 0).</summary>
        public decimal TotalOutflows { get; set; }

        /// <summary>Calculado: OpeningCash + TotalSales - TotalReturns - TotalOutflows.</summary>
        public decimal ExpectedCash { get; set; }

        /// <summary>Dinero físico contado al cierre.</summary>
        public decimal ActualCash { get; set; }

        /// <summary>Calculado: ActualCash - ExpectedCash.</summary>
        public decimal Difference { get; set; }

        /// <summary>Estado del cuadre: Balanced, Surplus o Shortage.</summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = DailySummaryStatus.Balanced;

        public string? Notes { get; set; }

        public bool IsClosed { get; set; }

        public Location? Location { get; set; }
        public Organization? Organization { get; set; }

        public ICollection<DailySummaryInventoryItem> InventoryItems { get; set; } = new List<DailySummaryInventoryItem>();
    }
}
