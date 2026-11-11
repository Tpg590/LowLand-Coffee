using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CoffeeShop.WebRazorPages.Services;
using CoffeeShop.WebRazorPages.ViewModels.Engagement;
using System.Security.Claims;

namespace CoffeeShop.WebRazorPages.Pages.Customer;

/// <summary>
/// PageModel cho trang Kho Voucher Ưu Đãi (Customer Vouchers).
/// </summary>
public class VouchersModel : PageModel
{
    private readonly IEngagementHttpService _engagementService;

    public VouchersModel(IEngagementHttpService engagementService)
    {
        _engagementService = engagementService;
    }

    public IEnumerable<VoucherViewModel> Vouchers { get; set; } = new List<VoucherViewModel>();
    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var res = await _engagementService.GetActiveVouchersAsync();
            if (res.IsSuccess && res.Data != null)
            {
                var vouchersList = res.Data.ToList();

                // Nếu người dùng đã đăng nhập, lọc bỏ hoàn toàn các voucher đã sử dụng
                if (User.Identity?.IsAuthenticated == true)
                {
                    var token = User.FindFirst(CoffeeShop.WebRazorPages.Constants.AuthConstants.AccessTokenClaimType)?.Value
                             ?? HttpContext.Session.GetString(CoffeeShop.WebRazorPages.Constants.AuthConstants.JwtSessionKey)
                             ?? Request.Cookies["access_token"];

                    var usedRes = await _engagementService.GetMyUsedVoucherIdsAsync(token);
                    if (usedRes.IsSuccess && usedRes.Data != null && usedRes.Data.Any())
                    {
                        var usedSet = new HashSet<Guid>(usedRes.Data);
                        vouchersList = vouchersList.Where(v => !usedSet.Contains(v.Id)).ToList();
                    }
                }

                Vouchers = vouchersList;
            }
        }
        catch
        {
            Vouchers = new List<VoucherViewModel>();
        }

        return Page();
    }
}
