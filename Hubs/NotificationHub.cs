using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ShuttlOps.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            if (user != null)
            {
                var role = user.FindFirst(ClaimTypes.Role)?.Value;
                var section = user.FindFirst("Section")?.Value;
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Group by Role (e.g. "Role_Admin", "Role_GA", "Role_Security", "Role_Requestor", "Role_Section Approver")
                if (!string.IsNullOrEmpty(role))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{role}");
                }

                // Group by Department (e.g. "Dept_IT", "Dept_HR", "Dept_Finances", Dept_Accounting", Dept_HRW")
                if (!string.IsNullOrEmpty(section))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Dept_{section}");
                }

                // Group by user ID (e.g. "User_123", "User_456")
                if (!string.IsNullOrEmpty(userId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                }
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}