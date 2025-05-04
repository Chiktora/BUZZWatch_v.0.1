using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BuzzWatch.Api.Swagger
{
    public class DiagnosticsOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            try
            {
                // Add operation metadata to help diagnose issues
                if (context.MethodInfo.DeclaringType?.FullName != null)
                {
                    operation.Extensions["x-controller"] = new OpenApiString(context.MethodInfo.DeclaringType.FullName);
                }
                
                operation.Extensions["x-method"] = new OpenApiString(context.MethodInfo.Name);

                // For minimal API endpoints
                if (context.ApiDescription.ActionDescriptor.DisplayName != null)
                {
                    operation.Extensions["x-endpoint"] = new OpenApiString(context.ApiDescription.ActionDescriptor.DisplayName);
                }
                
                // Log discovered parameters
                foreach (var parameter in context.ApiDescription.ParameterDescriptions)
                {
                    Console.WriteLine($"Parameter: {parameter.Name}, Type: {parameter.Type}, Source: {parameter.Source}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DiagnosticsOperationFilter: {ex.Message}");
                // Don't throw - we want to continue even if diagnostic fails
            }
        }
    }
} 