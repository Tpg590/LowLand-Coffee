using System.ComponentModel.DataAnnotations;

namespace Engagement.API.DTOs.Loyalty;

/// <summary>
/// DTO thông tin hạng thành viên và các quyền lợi.
/// </summary>
public class LoyaltyTierDto
{
    public Guid Id { get; set; }
    public string TierCode { get; set; } = string.Empty;
    public string TierName { get; set; } = string.Empty;
    public int MinPoints { get; set; }
    public decimal PointMultiplier { get; set; }
    public decimal DiscountPercent { get; set; }
    public string? BenefitsDescription { get; set; }
}

/// <summary>
/// DTO hồ sơ khách hàng thân thiết kèm tiến trình nâng hạng.
/// </summary>
public class LoyaltyMemberProfileDto
{
    public Guid UserId { get; set; }
    public string TierCode { get; set; } = "BRONZE";
    public string TierName { get; set; } = "Đồng";
    public int CurrentPoints { get; set; }
    public int TotalAccumulatedPoints { get; set; }
    public decimal PointMultiplier { get; set; } = 1.0m;
    public decimal DiscountPercent { get; set; }
    public string? NextTierName { get; set; }
    public int? PointsNeededForNextTier { get; set; }
    public string? BenefitsDescription { get; set; }
}

/// <summary>
/// DTO yêu cầu tích điểm sau khi hoàn tất đơn hàng.
/// </summary>
public class AddPointsRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid OrderId { get; set; }

    [Range(0, 100000000, ErrorMessage = "Giá trị đơn hàng không hợp lệ")]
    public decimal OrderAmount { get; set; }

    public string? Reason { get; set; }
}

/// <summary>
/// DTO kết quả sau khi tích điểm thành công.
/// </summary>
public class AddPointsResponse
{
    public Guid UserId { get; set; }
    public int PointsEarned { get; set; }
    public int CurrentPoints { get; set; }
    public int TotalAccumulatedPoints { get; set; }
    public string TierName { get; set; } = string.Empty;
    public bool DidTierUpgrade { get; set; }
}

/// <summary>
/// DTO yêu cầu quy đổi điểm tích lũy thành tiền giảm giá.
/// </summary>
public class RedeemPointsRequest
{
    [Required]
    public Guid UserId { get; set; }

    public Guid? OrderId { get; set; }

    [Range(1, 1000000, ErrorMessage = "Số điểm quy đổi phải từ 1 điểm trở lên")]
    public int PointsToRedeem { get; set; }
}

/// <summary>
/// DTO kết quả quy đổi điểm thành tiền giảm giá.
/// </summary>
public class RedeemPointsResponse
{
    public Guid UserId { get; set; }
    public int PointsRedeemed { get; set; }
    public decimal DiscountAmount { get; set; }
    public int RemainingPoints { get; set; }
}

/// <summary>
/// DTO thông tin lịch sử giao dịch biến động điểm.
/// </summary>
public class PointTransactionDto
{
    public Guid Id { get; set; }
    public Guid? OrderId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int PointsChanged { get; set; }
    public int BalanceAfter { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
