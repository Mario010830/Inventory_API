using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace APICore.API.Swagger
{
    /// <summary>
    /// Corrige la generación del esquema OpenAPI para endpoints con IFormFile.
    /// Evita el error 500 al cargar swagger.json.
    /// </summary>
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParams = context.ApiDescription.ParameterDescriptions
                .Where(p => p.Type == typeof(Microsoft.AspNetCore.Http.IFormFile) ||
                            p.Type == typeof(Microsoft.AspNetCore.Http.IFormFile[]))
                .ToList();

            if (fileParams.Count == 0)
                return;

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = fileParams.ToDictionary(
                                p => p.Name,
                                p => new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary",
                                    Description = "Archivo de imagen (JPEG, PNG, GIF, WebP, máx. 5 MB)"
                                }
                            ),
                            Required = fileParams.Select(p => p.Name).ToHashSet()
                        }
                    }
                }
            };

            // Remover parámetros duplicados que Swashbuckle pudo haber generado para IFormFile
            if (operation.Parameters != null)
            {
                var fileParamNames = fileParams.Select(p => p.Name).ToHashSet();
                var toRemove = operation.Parameters.Where(p => fileParamNames.Contains(p.Name)).ToList();
                foreach (var p in toRemove)
                    operation.Parameters.Remove(p);
            }
        }
    }
}
