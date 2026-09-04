using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShuttlOps.Services.Interfaces;
using System.Security.Claims;

namespace ShuttlOps.Controllers
{
    [Authorize]
    public class NotificationController(INotificationService notificationService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var notifications = await notificationService.GetUserNotificationsAsync(userId.Value);
            var unreadCount = await notificationService.GetUnreadCountAsync(userId.Value);

            return Json(new
            {
                success = true,
                data = notifications,
                unreadCount,
                userId
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var count = await notificationService.GetUnreadCountAsync(userId.Value);
            return Json(new { success = true, unreadCount = count });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            await notificationService.MarkAsReadAsync(id, userId.Value);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            await notificationService.MarkAllAsReadAsync(userId.Value);
            return Json(new { success = true });
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}