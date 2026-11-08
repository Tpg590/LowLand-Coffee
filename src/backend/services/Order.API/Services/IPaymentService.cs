using Microsoft.AspNetCore.Http;
using Order.API.DTOs;

namespace Order.API.Services;

/// <summary>
/// Interface định nghĩa các nghiệp vụ Thanh toán đơn hàng & tích hợp VNPay (Mục 28 & 29).
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Tạo đối tượng URL thanh toán & mã QR VNPay trực tiếp từ thực thể Order.
    /// </summary>
    VnPayPaymentResponseDto CreatePaymentUrl(Entities.Order order, string clientIp, string? customReturnUrl = null);

    /// <summary>
    /// [Mục 28 - Generate VNPay QR]: Tạo đường dẫn thanh toán và mã QR VNPay cho đơn hàng theo OrderId.
    /// </summary>
    Task<ApiResponse<VnPayPaymentResponseDto>> GenerateVnPayPaymentUrlAsync(
        Guid orderId, Guid? currentUserId, string currentUserRole, string clientIp, string? customReturnUrl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// [Mục 29A - Confirm Payment (VNPay IPN)]: Xử lý Webhook IPN tự động từ cổng VNPay.
    /// </summary>
    Task<VnPayIpnResponseDto> ProcessIpnCallbackAsync(IQueryCollection queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// [Mục 29B - Confirm Payment (VNPay Return)]: Xử lý kết quả trả về khi trình duyệt redirect từ VNPay.
    /// </summary>
    Task<VnPayReturnResultDto> ProcessPaymentReturnAsync(IQueryCollection queryParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// [Mục 29B-Sim - Confirm Payment (VNPay Sandbox Simulator)]:
    /// Hỗ trợ xác nhận thanh toán VNPay trực tiếp trên môi trường Test/Sandbox.
    /// </summary>
    Task<ApiResponse<VnPayReturnResultDto>> SimulateVnPayPaymentAsync(
        Guid orderId, Guid? currentUserId, string currentUserRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// [Mục 29C - Confirm Payment (Staff Cash Confirm)]: Nhân viên xác nhận thu tiền mặt cho đơn hàng.
    /// </summary>
    Task<ApiResponse<OrderPaymentDto>> ConfirmCashPaymentAsync(
        Guid orderId, Guid staffId, string staffRole, CashPaymentConfirmRequest? request, CancellationToken cancellationToken = default);
}

