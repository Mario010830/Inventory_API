namespace APICore.Common.DTO.Response
{
    /// <summary>
    /// Item de lista para tarjetas del panel (primary = texto principal, secondary = detalle).
    /// </summary>
    public class ListCardItemResponse
    {
        public string Primary { get; set; } = string.Empty;
        public string? Secondary { get; set; }
    }
}
