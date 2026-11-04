using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CoffeeShop.WebRazorPages.Services;
using CoffeeShop.WebRazorPages.ViewModels.Engagement;
using System.Security.Claims;

namespace CoffeeShop.WebRazorPages.Pages.Customer;

/// <summary>
/// PageModel cho màn hình Thành viên & Tích điểm (LowLand Coffee Loyalty).
/// Bắt buộc vai trò Khách hàng (Customer) để theo dõi thông tin hội viên.
/// </summary>
public class LoyaltyModel : PageModel
{
    private readonly IEngagementHttpService _engagementService;

    public LoyaltyModel(IEngagementHttpService engagementService)
    {
        _engagementService = engagementService;
    }

    /// <summary>
    /// Danh sách 4 cấp bậc hạng thẻ thành viên.
    /// </summary>
    public IEnumerable<LoyaltyTierViewModel> Tiers { get; set; } = new List<LoyaltyTierViewModel>();

    /// <summary>
    /// Xử lý HTTP GET: Nạp danh mục hạng thẻ thành viên và kiểm tra phân quyền tài khoản.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        // Kiểm tra phân quyền: Nếu đã xác thực qua Server Cookie nhưng không phải Customer -> điều hướng sang trang tương ứng
        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            if (!string.IsNullOrEmpty(role) && !role.Equals("Customer", StringComparison.OrdinalIgnoreCase))
            {
                if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToPage("/Admin/Users");
                }
                if (role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToPage("/Staff/Orders");
                }
            }
        }

        try
        {
            var res = await _engagementService.GetTiersAsync();
            if (res.IsSuccess && res.Data != null && res.Data.Any())
            {
                Tiers = res.Data;
            }
            else
            {
                Tiers = GetDefaultTiers();
            }
        }
        catch
        {
            // Dự phòng danh mục chuẩn khi dịch vụ nền đang khởi động
            Tiers = GetDefaultTiers();
        }

        return Page();
    }

    /// <summary>
    /// Cung cấp 4 cấp bậc thẻ chuẩn theo quy định hệ thống:
    /// - Đồng (0 điểm, x1.0, 0%)
    /// - Bạc (500 điểm, x1.1, 1%)
    /// - Vàng (1.500 điểm, x1.2, 2%)
    /// - Kim Cương (3.500 điểm, x1.3, 3%)
    /// </summary>
    private static List<LoyaltyTierViewModel> GetDefaultTiers()
    {
        return new List<LoyaltyTierViewModel>
        {
            new() {
                TierCode = "BRONZE",
                TierName = "Đồng",
                MinPoints = 0,
                PointMultiplier = 1.0m,
                DiscountPercent = 0.0m,
                BenefitsDescription = "Tích điểm chuẩn 10.000đ = 1 điểm. Chiết khấu 0%."
            },
            new() {
                TierCode = "SILVER",
                TierName = "Bạc",
                MinPoints = 500,
                PointMultiplier = 1.1m,
                DiscountPercent = 1.0m,
                BenefitsDescription = "Tích điểm x1.1 (+10% điểm tích lũy). Chiết khấu 1% trực tiếp mỗi đơn hàng."
            },
            new() {
                TierCode = "GOLD",
                TierName = "Vàng",
                MinPoints = 1500,
                PointMultiplier = 1.2m,
                DiscountPercent = 2.0m,
                BenefitsDescription = "Tích điểm x1.2 (+20% điểm tích lũy). Chiết khấu 2% trực tiếp mỗi đơn hàng."
            },
            new() {
                TierCode = "DIAMOND",
                TierName = "Kim Cương",
                MinPoints = 3500,
                PointMultiplier = 1.3m,
                DiscountPercent = 3.0m,
                BenefitsDescription = "Tích điểm x1.3 (+30% điểm tích lũy). Chiết khấu 3% trực tiếp mỗi đơn hàng."
            }
        };
    }
}
