using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Order.API.DTOs;
using Order.API.Services.Interfaces;

namespace Order.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IVnPayService _vnPayService;

    public PaymentController(IPaymentService paymentService, IVnPayService vnPayService)
    {
        _paymentService = paymentService;
        _vnPayService = vnPayService;
    }

    private Guid GetCurrentUserId()
    {
        var claimVal = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value
                       ?? User.FindFirst("CustomerId")?.Value
                       ?? User.FindFirst("id")?.Value;

        return Guid.TryParse(claimVal, out var guid) ? guid : Guid.Empty;
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value
               ?? User.FindFirst("role")?.Value
               ?? "Customer";
    }

    private string GetClientIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.ToString().Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
    }

    /// <summary>
    /// Chức năng 6A: Tạo đường dẫn / mã QR VNPay cho đơn hàng chưa thanh toán (từ Request Body)
    /// Áp dụng Strict Rate Limiting (tối đa 5 lần thử trong 10-15 phút)
    /// </summary>
    [HttpPost("vnpay/create-url")]
    [Authorize]
    [EnableRateLimiting("StrictPolicy")]
    [ProducesResponseType(typeof(ApiResponse<VnPayPaymentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VnPayPaymentResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VnPayPaymentResponseDto>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateVnPayUrl([FromBody] CreateVnPayPaymentRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();
        var clientIp = GetClientIpAddress();

        var result = await _paymentService.GenerateVnPayQrAsync(request.OrderId, userId, userRole, clientIp, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Chức năng 6B: Tạo mã QR VNPay theo Order ID trên Route
    /// Áp dụng Strict Rate Limiting (tối đa 5 lần thử trong 10-15 phút)
    /// </summary>
    [HttpPost("{orderId:guid}/vnpay-qr")]
    [Authorize]
    [EnableRateLimiting("StrictPolicy")]
    [ProducesResponseType(typeof(ApiResponse<VnPayPaymentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VnPayPaymentResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateVnPayQr([FromRoute] Guid orderId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();
        var clientIp = GetClientIpAddress();

        var result = await _paymentService.GenerateVnPayQrAsync(orderId, userId, userRole, clientIp, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Chức năng 7A: Webhook IPN từ VNPay (Server-to-Server, xác thực checksum HMACSHA512 & Idempotency)
    /// </summary>
    [HttpGet("vnpay-ipn")]
    [AllowAnonymous]
    [Produces("application/json")]
    public async Task<IActionResult> VnPayIpn(CancellationToken cancellationToken)
    {
        var ipnResult = await _vnPayService.ProcessIpnCallbackAsync(Request.Query, cancellationToken);
        return Ok(ipnResult);
    }

    /// <summary>
    /// Chức năng 7B: Return URL sau khi khách thanh toán xong trên cổng VNPay và redirect về
    /// </summary>
    [HttpGet("vnpay-return")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<VnPayReturnResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VnPayReturn(CancellationToken cancellationToken)
    {
        var returnResult = await _vnPayService.ProcessPaymentReturnAsync(Request.Query, cancellationToken);
        if (returnResult.IsSuccess)
        {
            return Ok(ApiResponse<VnPayReturnResultDto>.Success(returnResult, returnResult.Message));
        }

        return BadRequest(ApiResponse<VnPayReturnResultDto>.Fail(returnResult.Message, 400));
    }

    /// <summary>
    /// Chức năng 7C: Staff / Admin xác nhận thanh toán tiền mặt tại quầy
    /// </summary>
    [HttpPut("{orderId:guid}/cash-confirm")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(ApiResponse<OrderPaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderPaymentDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmCashPayment(
        [FromRoute] Guid orderId, [FromBody] CashPaymentConfirmRequest? request, CancellationToken cancellationToken)
    {
        var staffId = GetCurrentUserId();
        var staffRole = GetCurrentUserRole();

        var result = await _paymentService.ConfirmCashPaymentAsync(orderId, staffId, staffRole, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
