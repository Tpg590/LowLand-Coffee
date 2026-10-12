using Order.API.DTOs;

namespace Order.API.Hubs;

public interface IOrderNotificationService
{
    Task NotifyOrderCreatedAsync(OrderDetailDto order);
    Task NotifyOrderStatusChangedAsync(Guid orderId, string orderCode, Guid userId, string newStatus, string? cancelReason = null);
    Task NotifyPaymentConfirmedAsync(Guid orderId, string orderCode, Guid userId, string paymentMethod, string paymentStatus, decimal amount);
}
