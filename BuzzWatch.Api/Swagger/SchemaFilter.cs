using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace BuzzWatch.Api.Swagger
{
    public class SchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema?.Properties == null || context.Type == null)
                return;

            // Add type information to help identify schema conflicts
            if (context.Type.FullName != null)
            {
                schema.Extensions["x-dotnet-type"] = new OpenApiString(context.Type.FullName);
            }

            // Add example values for string properties
            foreach (var property in schema.Properties)
            {
                if (property.Value == null)
                    continue;
                    
                if (property.Value.Type == "string" && property.Value.Example == null)
                {
                    // Add some example values based on property name
                    if (property.Key.Contains("name", StringComparison.OrdinalIgnoreCase))
                    {
                        property.Value.Example = new OpenApiString("Example Name");
                    }
                    else if (property.Key.Contains("email", StringComparison.OrdinalIgnoreCase))
                    {
                        property.Value.Example = new OpenApiString("user@example.com");
                    }
                    else if (property.Key.Contains("key", StringComparison.OrdinalIgnoreCase))
                    {
                        property.Value.Example = new OpenApiString("api_key_example");
                    }
                    else if (property.Key.Contains("metric", StringComparison.OrdinalIgnoreCase))
                    {
                        property.Value.Example = new OpenApiString("temp_in");
                    }
                    else if (property.Key.Contains("operator", StringComparison.OrdinalIgnoreCase))
                    {
                        property.Value.Example = new OpenApiString(">=");
                    }
                }
            }
        }
    }
} 