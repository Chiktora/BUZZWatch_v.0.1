using BuzzWatch.Web.Models;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace BuzzWatch.Web.ViewComponents
{
    public class NotificationCenterViewComponent : ViewComponent
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<NotificationCenterViewComponent> _logger;

        public NotificationCenterViewComponent(ApiClient apiClient, ILogger<NotificationCenterViewComponent> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                // Only fetch if user is authenticated
                if (!User.Identity?.IsAuthenticated ?? false)
                {
                    return View(new NotificationCenterViewModel());
                }

                var notifications = await _apiClient.GetAsync<List<NotificationDto>>("/api/v1/notifications?limit=5");
                
                var viewModel = new NotificationCenterViewModel
                {
                    UnreadCount = notifications?.Count(n => !n.IsRead) ?? 0,
                    RecentNotifications = notifications?.OrderByDescending(n => n.Timestamp).Take(5).ToList() ?? new List<NotificationDto>()
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notifications for notification center");
                return View(new NotificationCenterViewModel());
            }
        }
    }
} 