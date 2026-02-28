using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// Respuesta para gráficos compuestos (barras + línea).
    /// </summary>
    public class ChartComposedResponse
    {
        public List<ChartComposedPointResponse> Data { get; set; } = new List<ChartComposedPointResponse>();
    }
}
