using System.Security.Claims;
using Engagement.API.DTOs.Common;
using Engagement.API.DTOs.Vouchers;
using Engagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Engagement.API.Controllers;

[ApiController]
[Route("api/vouchers")]
public class VouchersController : ControllerBase
{
    private readonly IVoucherService _voucherService;

    public VouchersController(IVoucherService voucherService)
    {
        _voucherService = voucherService;
    }

    /// <summary>
    /// Lấy danh sách Voucher đang còn hiệu lực công khai
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<VoucherDto>>>> GetActiveVouchers()
    {
        var vouchers = await _voucherService.GetAllVouchersAsync(isActiveOnly: true);
        return Ok(ApiResponse<IEnumerable<VoucherDto>>.Success(vouchers, "Lấy danh sách voucher thành công"));
    }

    /// <summary>
    /// Lấy toàn bộ danh sách Voucher cho quản trị viên (Admin)
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<VoucherDto>>>> GetAllVouchersForAdmin()
    {
        var vouchers = await _voucherService.GetAllVouchersAsync(isActiveOnly: false);
        return Ok(ApiResponse<IEnumerable<VoucherDto>>.Success(vouchers, "Lấy toàn bộ danh sách voucher thành công"));
    }

    /// <summary>
    /// Lấy chi tiết một Voucher theo ID
    /// </summary>
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
    /// Tạo mới Voucher (Admin)
    /// </summary>
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
    /// Cập nhật thông tin Voucher (Admin)
    /// </summary>
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
    /// Vô hiệu hóa hoặc Xóa Voucher (Admin)
    /// </summary>
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
    /// Kiểm tra mã Voucher hợp lệ cho đơn hàng tại màn hình Checkout
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ValidateVoucherResponse>>> ValidateVoucher([FromBody] ValidateVoucherRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<ValidateVoucherResponse>.Failure("Dữ liệu không hợp lệ", 400, ModelState));
        }

        // Tự động gán UserId từ JWT nếu có
        if (!request.UserId.HasValue && User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
    /// Ghi nhận áp dụng lượt sử dụng Voucher khi đơn hàng thanh toán thành công
    /// </summary>
    [HttpPost("apply-usage")]
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
}
