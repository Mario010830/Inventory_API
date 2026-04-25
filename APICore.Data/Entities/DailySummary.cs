using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APICore.Data.Entities
{
    public class DailySummary : BaseEntity
    {
        /// <summary>Día contable en Cuba (solo fecha civil; alinear con <c>request.Date</c> del API).</summary>
        [Required]
        public DateTime Date { get; set; }

        /// <summary>Inicio del periodo del turno en UTC (inclusive). Primer turno del día = 00:00 civil Cuba en UTC.</summary>
        [Required]
        public DateTime PeriodStart { get; set; }

        /// <summary>Cierre del cuadre en UTC (instante de cierre; el siguiente turno empieza después).</summary>
        public DateTime? ClosedAt { get; set; }

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

        /// <summary>Salidas de caja manuales del día (suma de <see cref="CashOutflow"/> con la misma fecha contable).</summary>
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

        public ICollection<PhysicalInventoryCount> PhysicalInventoryCounts { get; set; } = new List<PhysicalInventoryCount>();
    }
}
