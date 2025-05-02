using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace BuzzWatch.Web.Filters
{
    public class AuditLogAttribute : ActionFilterAttribute
    {
        private readonly string _action;
        private readonly string _entityType;

        public AuditLogAttribute(string action, string entityType = "")
        {
            _action = action;
            _entityType = entityType;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            
            // Only log successful actions
            if (context.Exception == null)
            {
                var auditLogService = context.HttpContext.RequestServices.GetService(typeof(IAuditLogService)) as IAuditLogService;
                
                if (auditLogService != null)
                {
                    var controllerName = context.Controller.GetType().Name.Replace("Controller", "");
                    var area = context.RouteData.Values["area"]?.ToString() ?? "";
                    var entityId = context.RouteData.Values["id"]?.ToString() ?? "";
                    var description = $"{_action} {_entityType} {(string.IsNullOrEmpty(entityId) ? "" : $"with ID {entityId}")}".Trim();
                    
                    // Use Task.Run to execute the async method from a sync context
                    Task.Run(() => auditLogService.LogActionAsync(_action, area, controllerName, description, entityId, _entityType));
                }
            }
        }
    }
} 