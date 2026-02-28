using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using APICore.Services.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class S3StorageService : IStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly S3StorageOptions _options;
        private readonly ILogger<S3StorageService> _logger;

        private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

        public S3StorageService(IOptions<S3StorageOptions> options, ILogger<S3StorageService> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(_options.Region)
            };

            if (!string.IsNullOrEmpty(_options.AccessKeyId) && !string.IsNullOrEmpty(_options.SecretAccessKey))
            {
                _s3Client = new AmazonS3Client(_options.AccessKeyId, _options.SecretAccessKey, config);
            }
            else
            {
                // Usa credenciales por defecto (variables de entorno, perfil AWS, IAM role, etc.)
                _s3Client = new AmazonS3Client(config);
            }
        }

        public async Task<string> UploadProductImageAsync(Stream fileStream, string fileName, string contentType)
        {
            if (!IsAllowedImageType(contentType))
                throw new ArgumentException($"Tipo de archivo no permitido: {contentType}. Solo se permiten: jpeg, png, gif, webp.");

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension))
                extension = GetExtensionFromContentType(contentType);

            var objectKey = $"{_options.ProductsFolder}/{Guid.NewGuid():N}{extension}";

            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey,
                InputStream = fileStream,
                ContentType = contentType
                // No usar CannedACL: buckets con "Object owner enforced" no permiten ACLs
            };

            await _s3Client.PutObjectAsync(request);

            var url = $"https://{_options.BucketName}.s3.{_options.Region}.amazonaws.com/{objectKey}";
            _logger.LogInformation("Imagen subida a S3: {ObjectKey}", objectKey);
            return url;
        }

        public async Task DeleteProductImageAsync(string objectKeyOrUrl)
        {
            var key = ExtractObjectKeyFromUrl(objectKeyOrUrl) ?? objectKeyOrUrl;

            var request = new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(request);
            _logger.LogInformation("Imagen eliminada de S3: {Key}", key);
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

        private string? ExtractObjectKeyFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.AbsolutePath.TrimStart('/');
            }
            catch
            {
                return null;
            }
        }
    }
}
