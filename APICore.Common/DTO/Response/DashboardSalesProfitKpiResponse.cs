using System;

namespace APICore.Common.DTO.Response
{
    /// <summary>KPI de ingresos por ventas confirmadas sin devolución (suma de totales en CUP).</summary>
    public class DashboardGrossSalesProfitResponse
    {
        /// <summary>Período aplicado: day, week, month o year.</summary>
        public string Period { get; set; } = null!;

        public DateTime FromUtc { get; set; }

        /// <summary>Límite superior exclusivo del rango.</summary>
        public DateTime ToUtcExclusive { get; set; }

        /// <summary>Suma de totales de venta (CUP) confirmadas sin devoluciones.</summary>
        public decimal GrossProfit { get; set; }

        public int OrderCount { get; set; }
    }

    /// <summary>KPI de ganancia tras costo de producto (CUP) en ventas confirmadas sin devolución.</summary>
    public class DashboardNetSalesProfitResponse
    {
        public string Period { get; set; } = null!;

        public DateTime FromUtc { get; set; }

        public DateTime ToUtcExclusive { get; set; }

        /// <summary>Ingresos (totales) menos costo de ventas: Σ Total − Σ (UnitCost × Quantity) por líneas.</summary>
        public decimal NetProfit { get; set; }

        public int OrderCount { get; set; }
    }
}
