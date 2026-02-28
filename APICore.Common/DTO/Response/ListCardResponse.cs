using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// Respuesta para listas del dashboard (top movimientos, stock bajo, últimos movimientos, etc.).
    /// </summary>
    public class ListCardResponse
    {
        public List<ListCardItemResponse> Data { get; set; } = new List<ListCardItemResponse>();
    }
}
