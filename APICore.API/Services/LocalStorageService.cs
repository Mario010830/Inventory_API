using APICore.Services;
using APICore.Services.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace APICore.API.Services
{
    /// <summary>
    /// Almacena imágenes de productos en disco local (wwwroot). Útil con ngrok o desarrollo.
    /// </summary>
    public class LocalStorageService : IStorageService
    {
        private readonly string _storagePath;
        private readonly string _requestPathPrefix;
        private readonly LocalStorageOptions _options;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<LocalStorageService> _logger;

        private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

        public LocalStorageService(
            IWebHostEnvironment env,
            IOptions<LocalStorageOptions> options,
            IHttpContextAccessor httpContextAccessor,
            ILogger<LocalStorageService> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            _storagePath = Path.Combine(webRoot, _options.StorageFolder.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar));
            _requestPathPrefix = "/" + _options.StorageFolder.TrimStart('/').TrimEnd('/') + "/";
        }

        public async Task<string> UploadProductImageAsync(Stream fileStream, string fileName, string contentType)
        {
            if (!IsAllowedImageType(contentType))
                throw new ArgumentException($"Tipo de archivo no permitido: {contentType}. Solo se permiten: jpeg, png, gif, webp.");

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension))
                extension = GetExtensionFromContentType(contentType);

            var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
            Directory.CreateDirectory(_storagePath);
            var fullPath = Path.Combine(_storagePath, uniqueFileName);

            await using (var fileStreamOut = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await fileStream.CopyToAsync(fileStreamOut);
            }

            var relativePath = _requestPathPrefix + uniqueFileName;
            var baseUrl = GetBaseUrl();
            var url = baseUrl + relativePath.TrimStart('/');
            _logger.LogInformation("Imagen guardada localmente: {Path}", fullPath);
            return url;
        }

        public async Task<string> UploadLocationImageAsync(Stream fileStream, string fileName, string contentType)
        {
            if (!IsAllowedImageType(contentType))
                throw new ArgumentException($"Tipo de archivo no permitido: {contentType}. Solo se permiten: jpeg, png, gif, webp.");

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension))
                extension = GetExtensionFromContentType(contentType);

            var locationsSubfolder = "locations";
            var fullStoragePath = Path.Combine(_storagePath, locationsSubfolder);
            var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
            Directory.CreateDirectory(fullStoragePath);
            var fullPath = Path.Combine(fullStoragePath, uniqueFileName);

            await using (var fileStreamOut = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await fileStream.CopyToAsync(fileStreamOut);
            }

            var relativePath = _requestPathPrefix + locationsSubfolder + "/" + uniqueFileName;
            var baseUrl = GetBaseUrl();
            var url = baseUrl + relativePath.TrimStart('/');
            _logger.LogInformation("Imagen de ubicación guardada: {Path}", fullPath);
            return url;
        }

        public Task DeleteLocationImageAsync(string objectKeyOrUrl)
        {
            var fileName = ExtractFileNameFromUrlOrKey(objectKeyOrUrl);
            if (string.IsNullOrEmpty(fileName)) return Task.CompletedTask;
            var locationsSubfolder = "locations";
            var fullPath = Path.Combine(_storagePath, locationsSubfolder, fileName);
            if (File.Exists(fullPath))
            {
                try { File.Delete(fullPath); _logger.LogInformation("Imagen de ubicación eliminada: {Path}", fullPath); }
                catch (Exception ex) { _logger.LogWarning(ex, "Error al eliminar imagen de ubicación: {Path}", fullPath); }
            }
            return Task.CompletedTask;
        }

        public Task DeleteProductImageAsync(string objectKeyOrUrl)
        {
            var fileName = ExtractFileNameFromUrlOrKey(objectKeyOrUrl);
            if (string.IsNullOrEmpty(fileName))
            {
                _logger.LogWarning("No se pudo extraer nombre de archivo de: {Input}", objectKeyOrUrl);
                return Task.CompletedTask;
            }

            var fullPath = Path.Combine(_storagePath, fileName);
            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("Imagen eliminada localmente: {Path}", fullPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error al eliminar archivo local: {Path}", fullPath);
                }
            }

            return Task.CompletedTask;
        }

        private string GetBaseUrl()
        {
            if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
                return _options.BaseUrl.TrimEnd('/') + "/";

            var context = _httpContextAccessor.HttpContext;
            if (context?.Request != null)
            {
                var request = context.Request;
                return $"{request.Scheme}://{request.Host}/";
            }

            return "/";
        }

        private static string? ExtractFileNameFromUrlOrKey(string objectKeyOrUrl)
        {
            try
            {
                if (Uri.TryCreate(objectKeyOrUrl, UriKind.Absolute, out var uri))
                    return Path.GetFileName(uri.AbsolutePath);
                return Path.GetFileName(objectKeyOrUrl.TrimStart('/', '\\'));
            }
            catch
            {
                return null;
            }
        }

        private static bool IsAllowedImageType(string contentType)
        {
            return Array.Exists(AllowedImageTypes, t => string.Equals(t, contentType, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetExtensionFromContentType(string contentType)
        {
            return contentType?.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
        }
    }
}
