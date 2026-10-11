using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Order.API.Hubs;

public class OrderHub : Hub
{
    public const string StaffGroup = "StaffRoom";

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var user = Context.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value
                         ?? user.FindFirst("CustomerId")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Cho phép khách lắng nghe kênh riêng của tài khoản mình
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            var role = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value;
            if (string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                // Nhân viên và Admin tự động vào nhóm quản trị để nhận toàn bộ thông báo đơn mới
                await Groups.AddToGroupAsync(Context.ConnectionId, StaffGroup);
            }
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinOrderTracking(string orderId)
    {
        if (!string.IsNullOrWhiteSpace(orderId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Order_{orderId}");
        }
    }

    public async Task LeaveOrderTracking(string orderId)
    {
        if (!string.IsNullOrWhiteSpace(orderId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Order_{orderId}");
        }
    }
}
