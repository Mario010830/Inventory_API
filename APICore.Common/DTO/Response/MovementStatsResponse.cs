namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// KPIs de la vista de movimientos.
    /// </summary>
    public class MovementStatsResponse
    {
        public int TotalMovements { get; set; }
        public decimal TotalMovementsTrend { get; set; }
        public int EntriesCount { get; set; }
        public decimal EntriesTrend { get; set; }
        public int ExitsCount { get; set; }
        public decimal ExitsTrend { get; set; }
        public int AdjustmentsCount { get; set; }
        public string AdjustmentsLabel { get; set; } = "Estable";
    }
}
