namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// Item para gráficos de dona (name + value; el frontend puede normalizar a porcentaje).
    /// </summary>
    public class ChartDonutItemResponse
    {
        public string Name { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}
