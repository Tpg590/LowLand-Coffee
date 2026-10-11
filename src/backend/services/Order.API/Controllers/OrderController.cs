using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Order.API.DTOs;
using Order.API.Services.Interfaces;

namespace Order.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
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

    /// <summary>
    /// Chức năng 1: Tạo đơn hàng mới (Customer đặt online / Staff tạo tại quầy)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<OrderDetailDto>.Fail("Dữ liệu không hợp lệ", 400, errors));
        }

        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var result = await _orderService.CreateOrderAsync(userId, userRole, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Chức năng 1B: Nhân viên tạo đơn hàng tại quầy POS (Dành riêng cho Staff / Admin)
    /// Hỗ trợ: Khách vãng lai (customerId = null) hoặc Khách thành viên (customerId = guid). Tự động sinh mã VNPay QR nếu chọn VNPay.
    /// </summary>
    [HttpPost("pos")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePosOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<OrderDetailDto>.Fail("Dữ liệu không hợp lệ", 400, errors));
        }

        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var result = await _orderService.CreateOrderAsync(userId, userRole, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Chức năng 2A: Xem danh sách đơn cá nhân của Customer (có phân trang)
    /// </summary>
    [HttpGet("my-orders")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders([FromQuery] OrderFilterRequest filter, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var userRole = "Customer"; // Ép quyền xem chỉ của cá nhân

        var result = await _orderService.GetOrdersAsync(userId, userRole, filter, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Chức năng 2B: Staff / Admin xem toàn bộ đơn tại quán (có phân trang & lọc trạng thái)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrders([FromQuery] OrderFilterRequest filter, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var result = await _orderService.GetOrdersAsync(userId, userRole, filter, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Chức năng 3: Xem chi tiết 1 đơn hàng (BOLA / IDOR protection kiểm tra chặt chẽ)
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderDetail([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var result = await _orderService.GetOrderDetailAsync(id, userId, userRole, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Chức năng 4: Cập nhật trạng thái đơn hàng theo thứ tự: Tiếp nhận -> Pha chế -> Giao hàng -> Hoàn thành (Staff/Admin)
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Staff,Admin")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrderStatus(
        [FromRoute] Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<OrderDetailDto>.Fail("Dữ liệu không hợp lệ", 400, errors));
        }

        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var result = await _orderService.UpdateOrderStatusAsync(id, request, userId, userRole, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Chức năng 5: Hủy đơn hàng (Áp dụng Strict Rate Limiting tối đa 5 lần trong 10-15 phút)
    /// Customer chỉ hủy khi PENDING; Staff/Admin bắt buộc nhập lý do.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    [EnableRateLimiting("StrictPolicy")]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<OrderDetailDto>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CancelOrder(
        [FromRoute] Guid id, [FromBody] CancelOrderRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<OrderDetailDto>.Fail("Dữ liệu không hợp lệ", 400, errors));
        }

        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        var result = await _orderService.CancelOrderAsync(id, request, userId, userRole, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
