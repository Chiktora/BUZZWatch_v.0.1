using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BuzzWatch.Api.Swagger
{
    public static class SwaggerExtensions
    {
        /// <summary>
        /// Adds OpenAPI server information
        /// </summary>
        public static void AddOpenApiServer(this SwaggerGenOptions options, string url, string description)
        {
            options.AddServer(new OpenApiServer
            {
                Url = url,
                Description = description
            });
        }
        
        /// <summary>
        /// Configure default schema ID generation for types
        /// </summary>
        public static void ConfigureSchemaIds(this SwaggerGenOptions options)
        {
            options.CustomSchemaIds(type =>
            {
                // Just use fully qualified name for all types to avoid conflicts
                return type.FullName?.Replace("+", ".").Replace("BuzzWatch.", "") ?? type.Name;
            });
        }
    }
} 