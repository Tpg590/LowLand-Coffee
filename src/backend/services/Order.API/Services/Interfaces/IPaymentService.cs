using Order.API.DTOs;

namespace Order.API.Services.Interfaces;

public interface IPaymentService
{
    Task<ApiResponse<VnPayPaymentResponseDto>> GenerateVnPayQrAsync(
        Guid orderId, Guid? currentUserId, string currentUserRole, string clientIp, CancellationToken cancellationToken = default);

    Task<ApiResponse<OrderPaymentDto>> ConfirmCashPaymentAsync(
        Guid orderId, Guid staffId, string staffRole, CashPaymentConfirmRequest? request, CancellationToken cancellationToken = default);
}
