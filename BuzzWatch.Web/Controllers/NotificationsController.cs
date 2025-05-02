using BuzzWatch.Web.Models;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuzzWatch.Web.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(ApiClient apiClient, ILogger<NotificationsController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var notifications = await _apiClient.GetAsync<List<NotificationDto>>("/api/v1/notifications");
                return View(new NotificationListViewModel 
                { 
                    Notifications = notifications ?? new List<NotificationDto>(),
                    UnreadCount = notifications?.Count(n => !n.IsRead) ?? 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notifications");
                return View(new NotificationListViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            try
            {
                await _apiClient.PostAsync<object>($"/api/v1/notifications/{id}/read", null);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                await _apiClient.PostAsync<object>("/api/v1/notifications/read-all", null);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _apiClient.DeleteAsync($"/api/v1/notifications/{id}");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return RedirectToAction(nameof(Index));
            }
        }

        // AJAX endpoint to get notifications count for navbar
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var notifications = await _apiClient.GetAsync<List<NotificationDto>>("/api/v1/notifications");
                int count = notifications?.Count(n => !n.IsRead) ?? 0;
                return Json(new { count });
            }
            catch (Exception)
            {
                return Json(new { count = 0 });
            }
        }
    }
} 