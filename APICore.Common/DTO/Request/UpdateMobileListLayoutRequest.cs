namespace APICore.Common.DTO.Request
{
    /// <summary>
    /// Preferencia de diseño para listas en móvil. Vacío o null borra la preferencia (volver a mostrar el asistente).
    /// Valores permitidos: <c>table</c> o <c>comfortable</c>. Las capturas del popup son del front; la API solo persiste la elección.
    /// </summary>
    public class UpdateMobileListLayoutRequest
    {
        public string? Layout { get; set; }
    }
}
