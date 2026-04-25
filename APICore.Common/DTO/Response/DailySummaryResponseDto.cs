using System;
using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    public class DailySummaryResponseDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        /// <summary>Inicio del periodo del turno (instante UTC en BD; mostrar en Cuba en UI).</summary>
        public DateTime PeriodStart { get; set; }
        /// <summary>Cierre del cuadre (instante UTC). null en datos legacy sin marca.</summary>
        public DateTime? ClosedAt { get; set; }
        public int LocationId { get; set; }
        public int OrganizationId { get; set; }
        public decimal OpeningCash { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal TotalOutflows { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal ActualCash { get; set; }
        public decimal Difference { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool IsClosed { get; set; }
        public List<DailySummaryInventoryItemDto> InventoryItems { get; set; } = new();

        /// <summary>Retiros de caja registrados para la misma fecha y ubicación del cuadre.</summary>
        public List<CashOutflowResponseDto> CashOutflows { get; set; } = new();

        /// <summary>Suma valorizada de sobrantes físicos (conteo finalizado). Informativo; no altera el cuadre de caja.</summary>
        public decimal? PhysicalCountTotalSurplusValued { get; set; }

        /// <summary>Suma valorizada de faltantes físicos (valor absoluto). Informativo.</summary>
        public decimal? PhysicalCountTotalShortageValued { get; set; }

        /// <summary>Impacto neto valorizado (sobrantes − faltantes en dinero). Informativo.</summary>
        public decimal? PhysicalCountNetValuedImpact { get; set; }
    }
}
