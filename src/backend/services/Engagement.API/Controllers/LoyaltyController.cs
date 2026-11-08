using System.Security.Claims;
using Engagement.API.DTOs.Common;
using Engagement.API.DTOs.Loyalty;
using Engagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Engagement.API.Controllers;

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
    /// Lấy danh sách 4 Hạng thẻ thành viên và các đặc quyền (Public / Customer / Staff / Admin)
    /// </summary>
    [HttpGet("tiers")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<LoyaltyTierDto>>>> GetTiers()
    {
        var tiers = await _loyaltyService.GetAllTiersAsync();
        return Ok(ApiResponse<IEnumerable<LoyaltyTierDto>>.Success(tiers, "Lấy danh sách hạng thành viên thành công"));
    }

    /// <summary>
    /// Xem điểm tích lũy & hạng thẻ của Khách hàng hiện tại
    /// Hỗ trợ endpoint: GET /api/loyalty/me
    /// </summary>
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
    /// Xem điểm tích lũy & hạng thẻ theo UserId cụ thể (Dành cho Staff POS tra cứu khách / Order.API / Admin)
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    [Authorize(Roles = "Customer,Staff,Admin")]
    public async Task<ActionResult<ApiResponse<LoyaltyMemberProfileDto>>> GetUserProfile(Guid userId)
    {
        var profile = await _loyaltyService.GetMemberProfileAsync(userId);
        return Ok(ApiResponse<LoyaltyMemberProfileDto>.Success(profile, "Lấy thông tin thành viên thành công"));
    }

    /// <summary>
    /// Tích điểm thành viên khi đơn hàng hoàn thành (Gọi từ Order.API hoặc Nhân viên quầy)
    /// Nhân hệ số điểm (PointMultiplier) tự động theo hạng thẻ của khách (Đồng x1.0, Bạc x1.1, Vàng x1.2, Kim Cương x1.3)
    /// </summary>
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
    /// Quy đổi điểm tích lũy để trừ tiền hóa đơn tại quầy hoặc lúc Checkout (1 điểm = 1.000 VNĐ)
    /// </summary>
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
    /// Lấy lịch sử giao dịch điểm tích lũy của người dùng hiện tại
    /// </summary>
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
    /// Lấy lịch sử giao dịch điểm theo UserId cụ thể (Staff / Admin)
    /// </summary>
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
