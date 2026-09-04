using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShuttlOps.DTOs;
using ShuttlOps.Hubs;
using ShuttlOps.Models.Temp;
using ShuttlOps.Services.Interfaces;
using ShuttlOps.Models;

namespace ShuttlOps.Services.MainServices
{
    public class NotificationService(
        IHubContext<NotificationHub> hubContext,
        ShuttlOpsDbContext dbContext,
        ILogger<NotificationService> logger
    ) : INotificationService
    {
        public async Task SendToUserAsync(string userId, NotificationMessageDTO notification)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || notification == null) return;


                    var user = await dbContext.UserTables
                        .Where(u => u.UserName == userId)
                        .Select(u => u.Id)
                        .FirstOrDefaultAsync();
                    if (user == 0) { logger.LogWarning("SendToUserAsync: No user found with UserName={UserId} in UserTables. Notification NOT saved.", userId); return; }
                   

                await SaveNotificationAsync(user, notification);

                var groups = new[] { $"User_{user}" };
                await hubContext.Clients.Groups(groups).SendAsync("ReceiveNotification", notification);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
            }
        }
        public async Task SendToRoleAsync(string role, NotificationMessageDTO notification)
        {
            try
            {
                if (string.IsNullOrEmpty(role) || notification == null) return;

                var users = await dbContext.UserTables
                    .Where(u => u.Role.RoleName == role)
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var uid in users)
                    await SaveNotificationAsync(uid, notification);

                var groups = new[] { $"Role_{role}" };
                await hubContext.Clients.Groups(groups).SendAsync("ReceiveNotification", notification);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send notification to role {Role}", role);
            }
        }

        public async Task SendToRolesAsync(IEnumerable<string> roles, NotificationMessageDTO notification)
        {
            try
            {
                if (roles == null || notification == null) return;

                var roleList = roles.ToList();
                var users = await dbContext.UserTables
                    .Where(u => roleList.Contains(u.Role.RoleName))
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var uid in users)
                    await SaveNotificationAsync(uid, notification);

                var groups = roleList.Select(r => $"Role_{r}").ToList();
                await hubContext.Clients.Groups(groups).SendAsync("ReceiveNotification", notification);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send notification to roles");
            }
        }

        public async Task SendToDepartmentAsync(string department, NotificationMessageDTO notification)
        {
            try
            {
                if (string.IsNullOrEmpty(department) || notification == null) return;

                var users = await dbContext.UserTables
                    .Where(u => u.Department.DepartmentName == department)
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var uid in users)
                    await SaveNotificationAsync(uid, notification);

                var groups = new[] { $"Dept_{department}" };
                await hubContext.Clients.Groups(groups).SendAsync("ReceiveNotification", notification);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send notification to department {Department}", department);
            }
        }


        public async Task SendToRoleInDepartmentAsync(string role, string department, NotificationMessageDTO notification)
        {
            try
            {
                if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(department) || notification == null) return;

                var users = await dbContext.UserTables
                    .Where(u => u.Role.RoleName == role && u.Department.DepartmentName == department)
                    .Select(u => u.Id)
                    .ToListAsync();

                foreach (var uid in users)
                    await SaveNotificationAsync(uid, notification);

                var groups = new[] { $"Role_{role}" };
                await hubContext.Clients.Groups(groups).SendAsync("ReceiveNotification", notification);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send notification to role {Role} in department {Department}", role, department);
            }
        }
        public async Task BroadcastAsync(NotificationMessageDTO notification)
        {
            try
            {
                if (notification == null) return;
                await hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to broadcast notification");
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId, int take = 20)
        {
            return await dbContext.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await dbContext.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            var notif = await dbContext.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);
            if (notif != null)
            {
                notif.IsRead = true;
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var unread = await dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            foreach (var n in unread)
                n.IsRead = true;
            await dbContext.SaveChangesAsync();
        }

        private async Task SaveNotificationAsync(int userId, NotificationMessageDTO notification)
        {
            var entity = new Notification
            {
                UserId = userId,
                Title = notification.Title,
                Message = notification.Message,
                Category = notification.Category ?? "General",
                TicketNumber = notification.TicketNumber,
                Url = notification.Url ?? "#",
                IsRead = false,
                CreatedAt = DateTime.Now
            };
            dbContext.Notifications.Add(entity);
            await dbContext.SaveChangesAsync();
        }
    }
}
