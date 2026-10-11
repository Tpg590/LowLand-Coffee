using Microsoft.AspNetCore.SignalR;
using Order.API.DTOs;

namespace Order.API.Hubs;

public class OrderNotificationService : IOrderNotificationService
{
    private readonly IHubContext<OrderHub> _hubContext;
    private readonly ILogger<OrderNotificationService> _logger;

    public OrderNotificationService(IHubContext<OrderHub> hubContext, ILogger<OrderNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyOrderCreatedAsync(OrderDetailDto order)
    {
        try
        {
            // Bắn cho Staff/Admin
            await _hubContext.Clients.Group(OrderHub.StaffGroup).SendAsync("OrderCreated", order);

            // Bắn cho chính Customer
            await _hubContext.Clients.Group($"User_{order.UserId}").SendAsync("OrderCreated", order);

            _logger.LogInformation("SignalR: Broadcasted OrderCreated for Order {OrderCode}", order.OrderCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR: Failed to broadcast OrderCreated for {OrderCode}", order.OrderCode);
        }
    }

    public async Task NotifyOrderStatusChangedAsync(Guid orderId, string orderCode, Guid userId, string newStatus, string? cancelReason = null)
    {
        try
        {
            var payload = new
            {
                OrderId = orderId,
                OrderCode = orderCode,
                UserId = userId,
                NewStatus = newStatus,
                CancelReason = cancelReason,
                UpdatedAt = DateTime.UtcNow
            };

            // Bắn cho phòng Staff/Admin
            await _hubContext.Clients.Group(OrderHub.StaffGroup).SendAsync("OrderStatusUpdated", payload);

            // Bắn cho khách hàng
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("OrderStatusUpdated", payload);

            // Bắn cho những ai đang theo dõi riêng đơn hàng này
            await _hubContext.Clients.Group($"Order_{orderId}").SendAsync("OrderStatusUpdated", payload);

            _logger.LogInformation("SignalR: Broadcasted OrderStatusUpdated for Order {OrderCode} -> {Status}", orderCode, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR: Failed to broadcast OrderStatusUpdated for {OrderCode}", orderCode);
        }
    }

    public async Task NotifyPaymentConfirmedAsync(Guid orderId, string orderCode, Guid userId, string paymentMethod, string paymentStatus, decimal amount)
    {
        try
        {
            var payload = new
            {
                OrderId = orderId,
                OrderCode = orderCode,
                UserId = userId,
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentStatus,
                Amount = amount,
                PaidAt = DateTime.UtcNow
            };

            await _hubContext.Clients.Group(OrderHub.StaffGroup).SendAsync("PaymentConfirmed", payload);
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("PaymentConfirmed", payload);
            await _hubContext.Clients.Group($"Order_{orderId}").SendAsync("PaymentConfirmed", payload);

            _logger.LogInformation("SignalR: Broadcasted PaymentConfirmed for Order {OrderCode} -> {PaymentStatus}", orderCode, paymentStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR: Failed to broadcast PaymentConfirmed for {OrderCode}", orderCode);
        }
    }
}
