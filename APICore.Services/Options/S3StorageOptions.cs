namespace APICore.Services.Options
{
    /// <summary>
    /// Configuración para el almacenamiento de imágenes en AWS S3.
    /// </summary>
    public class S3StorageOptions
    {
        public const string SectionName = "S3Storage";

        /// <summary>Nombre del bucket S3.</summary>
        public string BucketName { get; set; } = string.Empty;

        /// <summary>Región de AWS (ej: us-east-2).</summary>
        public string Region { get; set; } = "us-east-2";

        /// <summary>Carpeta base dentro del bucket para imágenes de productos.</summary>
        public string ProductsFolder { get; set; } = "products";

        /// <summary>Clave de acceso AWS (usar User Secrets o variables de entorno en producción).</summary>
        public string? AccessKeyId { get; set; }

        /// <summary>Clave secreta AWS (usar User Secrets o variables de entorno en producción).</summary>
        public string? SecretAccessKey { get; set; }
    }
}
