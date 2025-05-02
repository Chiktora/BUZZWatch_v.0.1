using BuzzWatch.Web.Models.Admin;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BuzzWatch.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AuditLogController : Controller
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuditLogController> _logger;

        public AuditLogController(IAuditLogService auditLogService, ILogger<AuditLogController> logger)
        {
            _auditLogService = auditLogService;
            _logger = logger;
        }

        // GET: /Admin/AuditLog
        public async Task<IActionResult> Index()
        {
            var recentLogs = await _auditLogService.GetRecentLogsAsync(20);
            return View(recentLogs);
        }

        // GET: /Admin/AuditLog/Search
        public async Task<IActionResult> Search(string term, int page = 1)
        {
            ViewData["SearchTerm"] = term;
            
            if (string.IsNullOrWhiteSpace(term))
                return RedirectToAction(nameof(Index));

            var results = await _auditLogService.SearchLogsAsync(term, page);
            return View("Index", results);
        }

        // GET: /Admin/AuditLog/ByDate
        public async Task<IActionResult> ByDate(DateTime? startDate, DateTime? endDate, int page = 1)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-7);
            var end = endDate ?? DateTime.UtcNow;
            
            ViewData["StartDate"] = start.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = end.ToString("yyyy-MM-dd");
            
            var results = await _auditLogService.GetLogsByDateRangeAsync(
                new DateTimeOffset(start), 
                new DateTimeOffset(end.AddDays(1).AddSeconds(-1)), 
                page);
                
            return View("Index", results);
        }

        // GET: /Admin/AuditLog/ByUser
        public async Task<IActionResult> ByUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return RedirectToAction(nameof(Index));
                
            ViewData["FilteredUser"] = userId;
            
            var results = await _auditLogService.GetLogsByUserAsync(userId);
            return View("Index", results);
        }

        // GET: /Admin/AuditLog/Details/5
        public IActionResult Details(Guid id)
        {
            // In a real implementation, we would fetch the specific log entry
            // For now, we'll just return a placeholder
            return View();
        }
    }
} 