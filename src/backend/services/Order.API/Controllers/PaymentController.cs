using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.API.DTOs;
using Order.API.Services;

namespace Order.API.Controllers;

/// <summary>
/// Controller xử lý nghiệp vụ Thanh toán (Payment Service - Phần III, mục 28 & 29 trong ma trận phân quyền).
/// Bao gồm:
/// - Mục 28: Generate VNPay QR (Customer & Staff)
/// - Mục 29: Confirm Payment (Webhook VNPay tự động xác nhận hoặc Staff xác nhận tiền mặt)
/// </summary>
[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// [Mục 28 - Generate VNPay QR]: Tạo URL thanh toán và mã QR VNPay cho đơn hàng.
    /// Phân quyền: Customer (tạo QR cho đơn cá nhân) và Staff (tạo QR tại quầy POS / bảng điều phối).
    /// </summary>
    [HttpPost("vnpay/create-url")]
    [Authorize(Roles = "Customer,Staff")]
    public async Task<IActionResult> CreateVnPayUrl([FromBody] CreateVnPayPaymentRequest request, CancellationToken cancellationToken)
    {
        if (request == null || request.OrderId == Guid.Empty)
        {
            return BadRequest(ApiResponse<VnPayPaymentResponseDto>.Fail("OrderId không hợp lệ.", 400));
        }

        var (userId, role) = GetCurrentUserContext();
        var clientIp = GetClientIpAddress();

        var result = await _paymentService.GenerateVnPayPaymentUrlAsync(
            request.OrderId, userId, role, clientIp, request.ReturnUrl, cancellationToken);

        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// [Mục 29B-Sim - Confirm Payment (VNPay Sandbox Simulator)]:
    /// Xác nhận thanh toán VNPay Sandbox trực tiếp (Phân quyền: Chỉ Staff).
    /// </summary>
    [HttpPost("{orderId:guid}/vnpay-simulate")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> SimulateVnPayPayment(Guid orderId, CancellationToken cancellationToken)
    {
        var (userId, role) = GetCurrentUserContext();
        var result = await _paymentService.SimulateVnPayPaymentAsync(orderId, userId, role, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// [Mục 29A - Confirm Payment (VNPay Webhook IPN)]:
    /// Đầu mối nhận thông báo kết quả thanh toán tự động (Server-to-Server IPN) từ cổng VNPay.
    /// Kiểm tra chữ ký số HMAC-SHA512 (SecureHash), đối chiếu số tiền và cập nhật trạng thái thanh toán.
    /// </summary>
    [HttpGet("vnpay-ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> VnPayIpn(CancellationToken cancellationToken)
    {
        var response = await _paymentService.ProcessIpnCallbackAsync(Request.Query, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// [Mục 29B - Confirm Payment (VNPay Return Callback)]:
    /// Đầu mối nhận kết quả khi trình duyệt của khách hàng redirect từ cổng VNPay về hệ thống.
    /// Xác thực chữ ký số HMAC-SHA512 và đồng bộ trạng thái đơn hàng ngay lập tức.
    /// </summary>
    [HttpGet("vnpay-return")]
    [AllowAnonymous]
    public async Task<IActionResult> VnPayReturn(CancellationToken cancellationToken)
    {
        var result = await _paymentService.ProcessPaymentReturnAsync(Request.Query, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<VnPayReturnResultDto>.Fail(result.Message, 400));
        }

        return Ok(ApiResponse<VnPayReturnResultDto>.Success(result, result.Message));
    }

    /// <summary>
    /// [Mục 29C - Confirm Payment (Staff Cash Confirm)]:
    /// Nhân viên (Staff) xác nhận đã thu tiền mặt cho đơn hàng tại quầy hoặc đơn COD.
    /// Phân quyền: Chỉ Staff.
    /// </summary>
    [HttpPut("{orderId:guid}/cash-confirm")]
    [HttpPut("{orderId:guid}/confirm-cash")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> ConfirmCashPayment(
        Guid orderId,
        [FromBody] CashPaymentConfirmRequest? request,
        CancellationToken cancellationToken)
    {
        var (userId, role) = GetCurrentUserContext();
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<OrderPaymentDto>.Fail("Không xác định được danh tính nhân viên.", 401));
        }

        var result = await _paymentService.ConfirmCashPaymentAsync(orderId, userId.Value, role, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    private (Guid? userId, string role) GetCurrentUserContext()
    {
        // Ưu tiên đọc Header do API Gateway đã xác thực JWT và chuyển tiếp xuống
        var userIdStr = Request.Headers["X-User-Id"].FirstOrDefault()
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub")
                     ?? User.FindFirstValue("uid")
                     ?? User.FindFirstValue("userId");

        Guid? userId = Guid.TryParse(userIdStr, out var parsedId) ? parsedId : null;

        var role = Request.Headers["X-User-Role"].FirstOrDefault()
                ?? User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role")
                ?? "Customer";

        return (userId, role);
    }

    private string GetClientIpAddress()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrWhiteSpace(ip) || ip == "::1")
        {
            return "127.0.0.1";
        }
        if (ip.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase))
        {
            ip = ip["::ffff:".Length..];
        }
        if (ip.Contains(':') || ip.StartsWith("172.") || ip.StartsWith("10.") || ip.StartsWith("192.168."))
        {
            return "127.0.0.1";
        }
        return ip;
    }
}
