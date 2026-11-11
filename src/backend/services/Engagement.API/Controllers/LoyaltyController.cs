using System.Security.Claims;
using Engagement.API.DTOs.Common;
using Engagement.API.DTOs.Loyalty;
using Engagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Engagement.API.Controllers;

/// <summary>
/// Controller quản lý chương trình khách hàng thân thiết và tích điểm (Loyalty Program).
/// </summary>
[ApiController]
[Route("api/loyalty")]
[Authorize]
public class LoyaltyController : ControllerBase
{
    private readonly ILoyaltyService _loyaltyService;

    public LoyaltyController(ILoyaltyService loyaltyService)
    {
        _loyaltyService = loyaltyService;
    }

    /// <summary>
    /// Lấy danh sách tất cả các hạng thành viên và quyền lợi tương ứng.
    /// </summary>
    /// <returns>Danh sách các hạng thành viên.</returns>
    [HttpGet("tiers")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<LoyaltyTierDto>>>> GetTiers()
    {
        var tiers = await _loyaltyService.GetAllTiersAsync();
        return Ok(ApiResponse<IEnumerable<LoyaltyTierDto>>.Success(tiers, "Lấy danh sách hạng thành viên thành công"));
    }

    /// <summary>
    /// Lấy thông tin hồ sơ hạng thành viên và điểm tích lũy của người dùng hiện tại.
    /// </summary>
    /// <returns>Hồ sơ thành viên gồm điểm khả dụng, điểm trọn đời và tiến trình nâng hạng.</returns>
    [HttpGet("me")]
    [Authorize(Roles = "Customer,Staff,Admin")]
    public async Task<ActionResult<ApiResponse<LoyaltyMemberProfileDto>>> GetMyTier()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<LoyaltyMemberProfileDto>.Failure("Không xác định được danh tính người dùng", 401));
        }

        var profile = await _loyaltyService.GetMemberProfileAsync(userId.Value);
        return Ok(ApiResponse<LoyaltyMemberProfileDto>.Success(profile, "Lấy thông tin hạng thành viên thành công"));
    }

    /// <summary>
    /// Lấy thông tin hồ sơ hạng thành viên theo mã định danh người dùng (dành cho Nhân viên/Admin tra cứu).
    /// </summary>
    /// <param name="userId">Mã định danh người dùng cần tra cứu.</param>
    /// <returns>Hồ sơ thành viên của người dùng tương ứng.</returns>
    [HttpGet("user/{userId:guid}")]
    [Authorize(Roles = "Customer,Staff,Admin")]
    public async Task<ActionResult<ApiResponse<LoyaltyMemberProfileDto>>> GetUserProfile(Guid userId)
    {
        var profile = await _loyaltyService.GetMemberProfileAsync(userId);
        return Ok(ApiResponse<LoyaltyMemberProfileDto>.Success(profile, "Lấy thông tin thành viên thành công"));
    }

    /// <summary>
    /// Tích điểm thành viên khi đơn hàng hoàn thành và tự động xét duyệt nâng hạng theo hệ số nhân điểm.
    /// </summary>
    /// <param name="request">Thông tin đơn hàng và số tiền thanh toán.</param>
    /// <returns>Kết quả tích điểm và trạng thái thăng hạng.</returns>
    [HttpPost("add-points")]
    [Authorize(Roles = "Customer,Staff,Admin")]
    public async Task<ActionResult<ApiResponse<AddPointsResponse>>> AddPoints([FromBody] AddPointsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<AddPointsResponse>.Failure("Dữ liệu không hợp lệ", 400, ModelState));
        }

        try
        {
            var result = await _loyaltyService.AddPointsAsync(request);
            string message = result.DidTierUpgrade
                ? $"Chúc mừng! Bạn đã tích thêm {result.PointsEarned} điểm và được thăng hạng lên {result.TierName}!"
                : $"Tích {result.PointsEarned} điểm thành công.";

            return Ok(ApiResponse<AddPointsResponse>.Success(result, message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AddPointsResponse>.Failure(ex.Message, 400));
        }
    }

    /// <summary>
    /// Quy đổi điểm tích lũy thành số tiền giảm giá trực tiếp cho đơn hàng.
    /// </summary>
    /// <param name="request">Thông tin yêu cầu quy đổi điểm.</param>
    /// <returns>Kết quả quy đổi và số tiền được giảm trừ.</returns>
    [HttpPost("redeem")]
    [Authorize(Roles = "Customer,Staff,Admin")]
    public async Task<ActionResult<ApiResponse<RedeemPointsResponse>>> RedeemPoints([FromBody] RedeemPointsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<RedeemPointsResponse>.Failure("Dữ liệu không hợp lệ", 400, ModelState));
        }

        if (request.UserId == Guid.Empty && User.Identity?.IsAuthenticated == true)
        {
            var tokenUserId = GetCurrentUserId();
            if (tokenUserId.HasValue)
            {
                request.UserId = tokenUserId.Value;
            }
        }

        try
        {
            var result = await _loyaltyService.RedeemPointsAsync(request);
            return Ok(ApiResponse<RedeemPointsResponse>.Success(result, $"Quy đổi {result.PointsRedeemed} điểm thành công, giảm {result.DiscountAmount:N0} VNĐ."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<RedeemPointsResponse>.Failure(ex.Message, 400));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<RedeemPointsResponse>.Failure(ex.Message, 400));
        }
    }

    /// <summary>
    /// Lấy lịch sử giao dịch biến động điểm của người dùng hiện tại.
    /// </summary>
    /// <param name="limit">Số lượng giao dịch tối đa cần lấy (mặc định 20).</param>
    /// <returns>Danh sách lịch sử giao dịch điểm.</returns>
    [HttpGet("transactions")]
    [Authorize(Roles = "Customer,Staff,Admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PointTransactionDto>>>> GetMyPointTransactions([FromQuery] int limit = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(ApiResponse<IEnumerable<PointTransactionDto>>.Failure("Không xác định được danh tính người dùng", 401));
        }

        var transactions = await _loyaltyService.GetPointTransactionsAsync(userId.Value, limit);
        return Ok(ApiResponse<IEnumerable<PointTransactionDto>>.Success(transactions, "Lấy lịch sử giao dịch điểm thành công"));
    }

    /// <summary>
    /// Lấy lịch sử giao dịch điểm theo mã người dùng cụ thể (dành cho Nhân viên/Admin tra cứu).
    /// </summary>
    /// <param name="userId">Mã định danh người dùng.</param>
    /// <param name="limit">Số lượng giao dịch tối đa cần lấy (mặc định 20).</param>
    /// <returns>Danh sách lịch sử giao dịch điểm.</returns>
    [HttpGet("transactions/{userId:guid}")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PointTransactionDto>>>> GetUserPointTransactions(Guid userId, [FromQuery] int limit = 20)
    {
        var transactions = await _loyaltyService.GetPointTransactionsAsync(userId, limit);
        return Ok(ApiResponse<IEnumerable<PointTransactionDto>>.Success(transactions, "Lấy lịch sử giao dịch điểm thành công"));
    }

    private Guid? GetCurrentUserId()
    {
        var claimValue = Request.Headers["X-User-Id"].FirstOrDefault()
                      ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? User.FindFirst("sub")?.Value
                      ?? User.FindFirst("id")?.Value
                      ?? User.FindFirst("userId")?.Value
                      ?? User.FindFirst("UserId")?.Value
                      ?? User.FindFirst("uid")?.Value;

        return Guid.TryParse(claimValue, out var guid) ? guid : null;
    }
}
