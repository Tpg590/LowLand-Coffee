using System.ComponentModel.DataAnnotations;

namespace CoffeeShop.WebRazorPages.ViewModels.Engagement;

public class LoyaltyTierViewModel
{
    public Guid Id { get; set; }
    public string TierCode { get; set; } = string.Empty;
    public string TierName { get; set; } = string.Empty;
    public int MinPoints { get; set; }
    public decimal PointMultiplier { get; set; }
    public decimal DiscountPercent { get; set; }
    public string? BenefitsDescription { get; set; }
}

public class LoyaltyMemberProfileViewModel
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

public class AddPointsRequestViewModel
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid OrderId { get; set; }

    [Range(0, 100000000, ErrorMessage = "Giá trị đơn hàng không hợp lệ")]
    public decimal OrderAmount { get; set; }

    public string? Reason { get; set; }
}

public class AddPointsResponseViewModel
{
    public Guid UserId { get; set; }
    public int PointsEarned { get; set; }
    public int CurrentPoints { get; set; }
    public int TotalAccumulatedPoints { get; set; }
    public string TierName { get; set; } = string.Empty;
    public bool DidTierUpgrade { get; set; }
}

public class RedeemPointsRequestViewModel
{
    [Required]
    public Guid UserId { get; set; }

    public Guid? OrderId { get; set; }

    [Range(1, 1000000, ErrorMessage = "Số điểm quy đổi phải từ 1 điểm trở lên")]
    public int PointsToRedeem { get; set; }

    public string? Reason { get; set; }
}

public class RedeemPointsResponseViewModel
{
    public Guid UserId { get; set; }
    public int PointsRedeemed { get; set; }
    public decimal DiscountAmount { get; set; }
    public int RemainingPoints { get; set; }
}

public class PointTransactionViewModel
{
    public Guid Id { get; set; }
    public Guid? OrderId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int PointsChanged { get; set; }
    public int BalanceAfter { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
