using System.Security.Claims;
using Engagement.API.DTOs.Common;
using Engagement.API.DTOs.Vouchers;
using Engagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Engagement.API.Controllers;

/// <summary>
/// Controller quản lý danh mục và nghiệp vụ áp dụng Voucher khuyến mãi.
/// </summary>
[ApiController]
[Route("api/vouchers")]
[Authorize]
public class VouchersController : ControllerBase
{
    private readonly IVoucherService _voucherService;

    public VouchersController(IVoucherService voucherService)
    {
        _voucherService = voucherService;
    }

    /// <summary>
    /// Lấy danh sách các voucher đang có hiệu lực công khai.
    /// </summary>
    /// <returns>Danh sách voucher hợp lệ để hiển thị cho khách hàng.</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<VoucherDto>>>> GetActiveVouchers()
    {
        var vouchers = await _voucherService.GetAllVouchersAsync(isActiveOnly: true);
        return Ok(ApiResponse<IEnumerable<VoucherDto>>.Success(vouchers, "Lấy danh sách voucher thành công"));
    }

    /// <summary>
    /// Lấy toàn bộ danh sách voucher trong hệ thống (dành cho Admin quản lý).
    /// </summary>
    /// <returns>Danh sách tất cả voucher bao gồm cả mã chưa kích hoạt hoặc đã hết hạn.</returns>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<VoucherDto>>>> GetAllVouchersForAdmin()
    {
        var vouchers = await _voucherService.GetAllVouchersAsync(isActiveOnly: false);
        return Ok(ApiResponse<IEnumerable<VoucherDto>>.Success(vouchers, "Lấy toàn bộ danh sách voucher thành công"));
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một voucher theo mã định danh (ID).
    /// </summary>
    /// <param name="id">Mã định danh của voucher.</param>
    /// <returns>Thông tin chi tiết của voucher.</returns>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> GetVoucherById(Guid id)
    {
        var voucher = await _voucherService.GetVoucherByIdAsync(id);
        if (voucher == null)
        {
            return NotFound(ApiResponse<VoucherDto>.Failure("Không tìm thấy voucher tương ứng", 404));
        }

        return Ok(ApiResponse<VoucherDto>.Success(voucher));
    }

    /// <summary>
    /// Tạo mới một mã voucher khuyến mãi trong hệ thống.
    /// </summary>
    /// <param name="request">Thông tin cấu hình voucher mới.</param>
    /// <returns>Thông tin voucher vừa được tạo.</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> CreateVoucher([FromBody] CreateVoucherRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<VoucherDto>.Failure("Dữ liệu đầu vào không hợp lệ", 400, ModelState));
        }

        try
        {
            var created = await _voucherService.CreateVoucherAsync(request);
            return StatusCode(201, ApiResponse<VoucherDto>.Success(created, "Tạo voucher thành công", 201));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<VoucherDto>.Failure(ex.Message, 400));
        }
    }

    /// <summary>
    /// Cập nhật thông tin cấu hình của một voucher đã có.
    /// </summary>
    /// <param name="id">Mã định danh của voucher cần cập nhật.</param>
    /// <param name="request">Thông tin cập nhật của voucher.</param>
    /// <returns>Thông tin voucher sau khi cập nhật.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<VoucherDto>>> UpdateVoucher(Guid id, [FromBody] UpdateVoucherRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<VoucherDto>.Failure("Dữ liệu đầu vào không hợp lệ", 400, ModelState));
        }

        var updated = await _voucherService.UpdateVoucherAsync(id, request);
        if (updated == null)
        {
            return NotFound(ApiResponse<VoucherDto>.Failure("Không tìm thấy voucher để cập nhật", 404));
        }

        return Ok(ApiResponse<VoucherDto>.Success(updated, "Cập nhật voucher thành công"));
    }

    /// <summary>
    /// Xóa hoặc vô hiệu hóa voucher khỏi hệ thống.
    /// </summary>
    /// <param name="id">Mã định danh của voucher cần xóa.</param>
    /// <returns>Trạng thái thực hiện xóa/vô hiệu hóa.</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteVoucher(Guid id)
    {
        var success = await _voucherService.DeleteVoucherAsync(id);
        if (!success)
        {
            return NotFound(ApiResponse<bool>.Failure("Không tìm thấy voucher để xóa", 404));
        }

        return Ok(ApiResponse<bool>.Success(true, "Vô hiệu hóa voucher thành công"));
    }

    /// <summary>
    /// Kiểm tra và thẩm định tính hợp lệ của mã voucher cho đơn hàng cụ thể.
    /// </summary>
    /// <param name="request">Thông tin mã voucher, giá trị đơn hàng và khách hàng.</param>
    /// <returns>Kết quả thẩm định kèm số tiền được giảm giá.</returns>
    [HttpPost("validate")]
    [Authorize(Roles = "Customer,Staff")]
    public async Task<ActionResult<ApiResponse<ValidateVoucherResponse>>> ValidateVoucher([FromBody] ValidateVoucherRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<ValidateVoucherResponse>.Failure("Dữ liệu không hợp lệ", 400, ModelState));
        }

        if ((!request.UserId.HasValue || request.UserId.Value == Guid.Empty) && User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = Request.Headers["X-User-Id"].FirstOrDefault()
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("sub")?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                request.UserId = userId;
            }
        }

        var result = await _voucherService.ValidateVoucherAsync(request);
        if (!result.IsValid)
        {
            return BadRequest(ApiResponse<ValidateVoucherResponse>.Failure(result.Message ?? "Mã giảm giá không hợp lệ", 400, result));
        }

        return Ok(ApiResponse<ValidateVoucherResponse>.Success(result, result.Message ?? "Áp dụng voucher thành công"));
    }

    /// <summary>
    /// Ghi nhận lượt sử dụng voucher khi đơn hàng được tạo hoặc thanh toán thành công.
    /// </summary>
    /// <param name="request">Thông tin đơn hàng và mã voucher đã áp dụng.</param>
    /// <returns>Trạng thái ghi nhận lượt sử dụng.</returns>
    [HttpPost("apply-usage")]
    [Authorize(Roles = "Customer,Staff")]
    public async Task<ActionResult<ApiResponse<bool>>> ApplyVoucherUsage([FromBody] ApplyVoucherUsageRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<bool>.Failure("Dữ liệu không hợp lệ", 400, ModelState));
        }

        var result = await _voucherService.ApplyVoucherUsageAsync(request);
        if (!result)
        {
            return BadRequest(ApiResponse<bool>.Failure("Không thể áp dụng voucher do mã không hợp lệ hoặc đã hết lượt", 400));
        }

        return Ok(ApiResponse<bool>.Success(true, "Ghi nhận sử dụng voucher thành công"));
    }

    /// <summary>
    /// Lấy danh sách ID các voucher mà người dùng hiện tại đã từng sử dụng.
    /// </summary>
    /// <returns>Danh sách ID các voucher đã dùng.</returns>
    [HttpGet("my-used-ids")]
    [Authorize(Roles = "Customer,Staff")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Guid>>>> GetMyUsedVoucherIds()
    {
        var userIdClaim = Request.Headers["X-User-Id"].FirstOrDefault()
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<IEnumerable<Guid>>.Failure("Không tìm thấy thông tin định danh người dùng", 401));
        }

        var usedIds = await _voucherService.GetUsedVoucherIdsByUserAsync(userId);
        return Ok(ApiResponse<IEnumerable<Guid>>.Success(usedIds, "Lấy danh sách ID voucher đã sử dụng thành công"));
    }

    /// <summary>
    /// Lấy danh sách ID các voucher mà một khách hàng cụ thể đã sử dụng (dành cho Nhân viên quầy tra cứu theo khách hàng).
    /// </summary>
    /// <param name="userId">Mã định danh khách hàng.</param>
    /// <returns>Danh sách ID các voucher khách hàng đã dùng.</returns>
    [HttpGet("used-ids/{userId:guid}")]
    [Authorize(Roles = "Customer,Staff,Admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Guid>>>> GetUsedVoucherIdsByUser(Guid userId)
    {
        var usedIds = await _voucherService.GetUsedVoucherIdsByUserAsync(userId);
        return Ok(ApiResponse<IEnumerable<Guid>>.Success(usedIds, "Lấy danh sách ID voucher đã sử dụng của khách hàng thành công"));
    }
}
