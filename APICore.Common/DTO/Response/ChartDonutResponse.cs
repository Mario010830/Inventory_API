using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// Respuesta para gráficos de dona (lista name/value).
    /// </summary>
    public class ChartDonutResponse
    {
        public List<ChartDonutItemResponse> Data { get; set; } = new List<ChartDonutItemResponse>();
    }
}
