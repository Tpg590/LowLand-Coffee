using Order.API.DTOs;

namespace Order.API.Services.Interfaces;

public interface IOrderService
{
    Task<ApiResponse<OrderDetailDto>> CreateOrderAsync(
        Guid currentUserId, string currentUserRole, CreateOrderRequest request, string? clientIp = null, CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<OrderDto>>> GetOrdersAsync(
        Guid? currentUserId, string currentUserRole, OrderFilterRequest filter, CancellationToken cancellationToken = default);

    Task<ApiResponse<OrderDetailDto>> GetOrderDetailAsync(
        Guid orderId, Guid? currentUserId, string currentUserRole, CancellationToken cancellationToken = default);

    Task<ApiResponse<OrderDetailDto>> UpdateOrderStatusAsync(
        Guid orderId, UpdateOrderStatusRequest request, Guid currentUserId, string currentUserRole, CancellationToken cancellationToken = default);

    Task<ApiResponse<OrderDetailDto>> CancelOrderAsync(
        Guid orderId, CancelOrderRequest request, Guid currentUserId, string currentUserRole, CancellationToken cancellationToken = default);
}
