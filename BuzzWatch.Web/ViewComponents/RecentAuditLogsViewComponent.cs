using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BuzzWatch.Web.ViewComponents
{
    public class RecentAuditLogsViewComponent : ViewComponent
    {
        private readonly IAuditLogService _auditLogService;

        public RecentAuditLogsViewComponent(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        public async Task<IViewComponentResult> InvokeAsync(int count = 5)
        {
            var logs = await _auditLogService.GetRecentLogsAsync(count);
            return View(logs);
        }
    }
} 