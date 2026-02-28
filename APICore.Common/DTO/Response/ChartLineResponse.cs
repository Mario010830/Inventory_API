using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// Respuesta para gráficos de línea o barras (lista de puntos).
    /// </summary>
    public class ChartLineResponse
    {
        public List<ChartDataPointResponse> Data { get; set; } = new List<ChartDataPointResponse>();
    }
}
