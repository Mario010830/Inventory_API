namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// Punto de datos para gráficos de línea o barras (eje X: label/date, eje Y: value).
    /// </summary>
    public class ChartDataPointResponse
    {
        public string Label { get; set; } = string.Empty;
        public string? Date { get; set; }
        public decimal Value { get; set; }
    }
}
