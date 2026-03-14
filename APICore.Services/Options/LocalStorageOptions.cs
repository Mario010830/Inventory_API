namespace APICore.Services.Options
{
    /// <summary>
    /// Configuración para almacenar imágenes en disco local (p. ej. con ngrok o desarrollo).
    /// </summary>
    public class LocalStorageOptions
    {
        public const string SectionName = "LocalStorage";

        /// <summary>
        /// Carpeta relativa a wwwroot donde se guardan las imágenes (ej: "uploads/products").
        /// </summary>
        public string StorageFolder { get; set; } = "uploads/products";

        /// <summary>
        /// URL base para generar enlaces (ej: https://tu-tunel.ngrok.io).
        /// Si está vacía, se usa la URL de la petición actual (recomendado con ngrok).
        /// </summary>
        public string? BaseUrl { get; set; }
    }
}
