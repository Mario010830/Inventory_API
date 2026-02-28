namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// Punto para gráficos compuestos (barras + línea): value = barras, lineValue = serie de la línea.
    /// </summary>
    public class ChartComposedPointResponse
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal LineValue { get; set; }
    }
}
