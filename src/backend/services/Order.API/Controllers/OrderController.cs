using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.API.DTOs;
using Order.API.Services;

namespace Order.API.Controllers;

/// <summary>
/// Controller xử lý toàn bộ nghiệp vụ Đơn hàng (Order Service - Phần III, mục 22 đến 27 trong ma trận phân quyền).
/// Phân quyền chuẩn: Chỉ có Customer và Staff tham gia vào Order & Payment Service.
/// </summary>
[ApiController]
[Route("api/orders")]
[Authorize(Roles = "Customer,Staff")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// [Mục 22 - Create Order]:
    /// - Customer đặt hàng online (UserId lấy từ JWT Token).
    /// - Staff tạo đơn trực tiếp tại quầy POS (cho phép gắn CustomerId / CustomerName của khách).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Customer,Staff")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var (userId, role) = GetCurrentUserContext();
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<OrderDetailDto>.Fail("Không xác định được danh tính người dùng từ Token.", 401));
        }

        var clientIp = GetClientIpAddress();
        var result = await _orderService.CreateOrderAsync(userId.Value, role, request, clientIp, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// [Mục 23 & 25 - View Order List & Search/Filter Order (Customer)]:
    /// Khách hàng xem danh sách đơn hàng cá nhân của chính mình, hỗ trợ lọc theo trạng thái và mã đơn.
    /// </summary>
    [HttpGet("my-orders")]
    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyOrders([FromQuery] OrderFilterRequest filter, CancellationToken cancellationToken)
    {
        var (userId, _) = GetCurrentUserContext();
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<PagedResult<OrderDto>>.Fail("Không xác định được danh tính khách hàng.", 401));
        }

        var result = await _orderService.GetOrdersAsync(userId.Value, "Customer", filter, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// [Mục 23 & 25 - View Order List & Search/Filter Order (Staff)]:
    /// Nhân viên xem toàn bộ danh sách đơn hàng trên hệ thống, hỗ trợ tìm kiếm mã đơn và lọc trạng thái.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GetAllOrders([FromQuery] OrderFilterRequest filter, CancellationToken cancellationToken)
    {
        var (userId, role) = GetCurrentUserContext();
        var result = await _orderService.GetOrdersAsync(userId, role, filter, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// [Mục 24 - View Order Detail]:
    /// Xem thông tin chi tiết các món (items), tùy chọn và giá tiền trong một đơn hàng.
    /// - Customer: chỉ được xem đơn của chính mình (chống lỗ hổng BOLA/IDOR).
    /// - Staff: được xem chi tiết mọi đơn hàng để phục vụ và điều phối.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Customer,Staff")]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var (userId, role) = GetCurrentUserContext();
        var result = await _orderService.GetOrderDetailAsync(id, userId, role, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// [Mục 26 - Update Order Status]:
    /// Nhân viên cập nhật trạng thái đơn hàng theo đúng quy trình tuần tự:
    /// Pending -> Processing -> Shipping -> Completed.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> UpdateOrderStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var (userId, role) = GetCurrentUserContext();
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<OrderDetailDto>.Fail("Không xác định được danh tính nhân viên.", 401));
        }

        var result = await _orderService.UpdateOrderStatusAsync(id, request, userId.Value, role, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// [Mục 27 - Cancel Order]:
    /// - Customer: Chỉ được hủy đơn hàng của chính mình khi đơn còn ở trạng thái mới (Pending).
    /// - Staff: Được phép hủy đơn hàng kèm theo lý do hủy bắt buộc.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [HttpPut("{id:guid}/cancel")]
    [Authorize(Roles = "Customer,Staff")]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        var (userId, role) = GetCurrentUserContext();
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<OrderDetailDto>.Fail("Không xác định được danh tính người dùng.", 401));
        }

        var result = await _orderService.CancelOrderAsync(id, request, userId.Value, role, cancellationToken);
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
