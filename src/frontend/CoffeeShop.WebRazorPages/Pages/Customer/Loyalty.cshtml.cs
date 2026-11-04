using Microsoft.AspNetCore.Mvc.RazorPages;
using CoffeeShop.WebRazorPages.Services;
using CoffeeShop.WebRazorPages.ViewModels.Engagement;

namespace CoffeeShop.WebRazorPages.Pages.Customer;

public class LoyaltyModel : PageModel
{
    private readonly IEngagementHttpService _engagementService;

    public LoyaltyModel(IEngagementHttpService engagementService)
    {
        _engagementService = engagementService;
    }

    public IEnumerable<LoyaltyTierViewModel> Tiers { get; set; } = new List<LoyaltyTierViewModel>();

    public async Task OnGetAsync()
    {
        try
        {
            var res = await _engagementService.GetTiersAsync();
            if (res.IsSuccess && res.Data != null)
            {
                Tiers = res.Data;
            }
        }
        catch
        {
            // Fallback default tiers in case API service is initializing
            Tiers = GetDefaultTiers();
        }

        if (!Tiers.Any())
        {
            Tiers = GetDefaultTiers();
        }
    }

    private static List<LoyaltyTierViewModel> GetDefaultTiers()
    {
        return new List<LoyaltyTierViewModel>
        {
            new() {
                TierCode = "BRONZE",
                TierName = "Đồng",
                MinPoints = 0,
                PointMultiplier = 1.0m,
                DiscountPercent = 0m,
                BenefitsDescription = "Hạng mặc định khi đăng ký tài khoản. Tích lũy 1 điểm cho mỗi 10.000 VNĐ chi tiêu."
            },
            new() {
                TierCode = "SILVER",
                TierName = "Bạc",
                MinPoints = 500,
                PointMultiplier = 1.2m,
                DiscountPercent = 5.0m,
                BenefitsDescription = "Tích điểm x1.2 lần (10.000đ = 1.2 điểm). Giảm 5% trực tiếp trên mỗi đơn hàng."
            },
            new() {
                TierCode = "GOLD",
                TierName = "Vàng",
                MinPoints = 1500,
                PointMultiplier = 1.5m,
                DiscountPercent = 10.0m,
                BenefitsDescription = "Tích điểm x1.5 lần (10.000đ = 1.5 điểm). Giảm 10% trực tiếp trên mỗi đơn hàng."
            },
            new() {
                TierCode = "DIAMOND",
                TierName = "Kim Cương",
                MinPoints = 3500,
                PointMultiplier = 2.0m,
                DiscountPercent = 15.0m,
                BenefitsDescription = "Đặc quyền VIP cao cấp nhất: Nhân đôi số điểm tích lũy (x2.0), giảm 15% trực tiếp trên mỗi đơn hàng."
            }
        };
    }
}
