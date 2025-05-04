using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Text;

namespace BuzzWatch.Api.Swagger
{
    public class ConflictingActionsResolver
    {
        public ApiDescription Resolve(IEnumerable<ApiDescription> descriptions)
        {
            // Log all conflicting endpoints for debugging
            var sb = new StringBuilder();
            sb.AppendLine("Conflicting API endpoints found:");
            
            foreach (var description in descriptions)
            {
                string controllerName = "Unknown";
                string actionName = "Unknown";
                
                if (description.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
                {
                    controllerName = controllerActionDescriptor.ControllerName;
                    actionName = controllerActionDescriptor.ActionName;
                }
                
                sb.AppendLine($"- {description.HttpMethod} {description.RelativePath} - Controller: {controllerName}, Action: {actionName}");
            }
            
            Console.WriteLine(sb.ToString());
            
            // Prioritize controller-based endpoints over minimal API endpoints
            var controllerEndpoint = descriptions.FirstOrDefault(d => 
                d.ActionDescriptor is ControllerActionDescriptor);
                
            if (controllerEndpoint != null)
            {
                return controllerEndpoint;
            }
            
            // If no controller-based endpoint, return the first one
            return descriptions.First();
        }
    }
} 