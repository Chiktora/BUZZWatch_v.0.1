using BuzzWatch.Web.Filters;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuzzWatch.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(ApiClient apiClient, ILogger<SettingsController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // GET: /Admin/Settings
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Admin/Settings/System
        public IActionResult System()
        {
            var model = new SystemSettingsViewModel
            {
                SmtpServer = "smtp.example.com",
                SmtpPort = 587,
                SmtpUsername = "notifications@example.com",
                SmtpRequiresAuth = true,
                EnableEmailNotifications = true,
                SiteTitle = "BuzzWatch",
                SystemTimeZone = "UTC"
            };
            
            return View(model);
        }

        // POST: /Admin/Settings/System
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuditLog("Update", "SystemSettings")]
        public IActionResult System(SystemSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Mock implementation - in a real scenario, this would save to a database
                TempData["SuccessMessage"] = "System settings updated successfully.";
                return RedirectToAction(nameof(System));
            }
            
            return View(model);
        }

        // GET: /Admin/Settings/Api
        public IActionResult Api()
        {
            var model = new ApiSettingsViewModel
            {
                ApiRateLimitPerMinute = 60,
                DefaultApiKeyExpiryDays = 365,
                EnableCors = true,
                AllowedOrigins = "https://example.com,https://api.example.com",
                EnableSwagger = true
            };
            
            return View(model);
        }

        // POST: /Admin/Settings/Api
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuditLog("Update", "ApiSettings")]
        public IActionResult Api(ApiSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Mock implementation - in a real scenario, this would save to a database
                TempData["SuccessMessage"] = "API settings updated successfully.";
                return RedirectToAction(nameof(Api));
            }
            
            return View(model);
        }

        // GET: /Admin/Settings/Alerts
        public IActionResult Alerts()
        {
            var model = new AlertSettingsViewModel
            {
                DefaultRepeatIntervalMinutes = 30,
                AlertResolveAfterMinutes = 60,
                SendEmailAlerts = true,
                SendSmsAlerts = false,
                MaxAlertsPerDay = 100
            };
            
            return View(model);
        }

        // POST: /Admin/Settings/Alerts
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuditLog("Update", "AlertSettings")]
        public IActionResult Alerts(AlertSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Mock implementation - in a real scenario, this would save to a database
                TempData["SuccessMessage"] = "Alert settings updated successfully.";
                return RedirectToAction(nameof(Alerts));
            }
            
            return View(model);
        }
    }

    public class SystemSettingsViewModel
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; } = string.Empty;
        public string? SmtpPassword { get; set; }
        public bool SmtpRequiresAuth { get; set; }
        public bool EnableEmailNotifications { get; set; }
        public string SiteTitle { get; set; } = string.Empty;
        public string SystemTimeZone { get; set; } = string.Empty;
    }

    public class ApiSettingsViewModel
    {
        public int ApiRateLimitPerMinute { get; set; }
        public int DefaultApiKeyExpiryDays { get; set; }
        public bool EnableCors { get; set; }
        public string AllowedOrigins { get; set; } = string.Empty;
        public bool EnableSwagger { get; set; }
    }

    public class AlertSettingsViewModel
    {
        public int DefaultRepeatIntervalMinutes { get; set; }
        public int AlertResolveAfterMinutes { get; set; }
        public bool SendEmailAlerts { get; set; }
        public bool SendSmsAlerts { get; set; }
        public int MaxAlertsPerDay { get; set; }
    }
} 