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

    public IEnumerable<LoyaltyTierViewModel> Tiers { get; set; } = new List<LoyaltyTierViewModel>();
    public async Task<IActionResult> OnGetAsync()
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
            Tiers = new List<LoyaltyTierViewModel>();
        }

        return Page();
    }
}
