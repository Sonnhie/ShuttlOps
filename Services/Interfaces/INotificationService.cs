using ShuttlOps.DTOs;
using ShuttlOps.Models.Temp;

namespace ShuttlOps.Services.Interfaces
{
    public interface INotificationService
    {
        Task SendToUserAsync(string userId, NotificationMessageDTO notification);
        Task SendToRoleAsync(string role, NotificationMessageDTO notification);
        Task SendToRolesAsync(IEnumerable<string> roles, NotificationMessageDTO notification);
        Task SendToDepartmentAsync(string department, NotificationMessageDTO notification);
        Task SendToRoleInDepartmentAsync(string role, string department, NotificationMessageDTO notification);
        Task SendToSectionHeadOfDepartmentAsync(string departmentName, NotificationMessageDTO notification);
        Task BroadcastAsync(NotificationMessageDTO notification);

        Task<List<Notification>> GetUserNotificationsAsync(int userId, int take = 20);
        Task<int> GetUnreadCountAsync(int userId);
        Task MarkAsReadAsync(int notificationId, int userId);
        Task MarkAllAsReadAsync(int userId);
    }
}
