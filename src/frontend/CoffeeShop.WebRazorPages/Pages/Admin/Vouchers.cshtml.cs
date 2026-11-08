using Microsoft.AspNetCore.Mvc.RazorPages;
using CoffeeShop.WebRazorPages.Services;
using CoffeeShop.WebRazorPages.ViewModels.Engagement;

namespace CoffeeShop.WebRazorPages.Pages.Admin;

public class VouchersModel : PageModel
{
    private readonly IEngagementHttpService _engagementService;

    public VouchersModel(IEngagementHttpService engagementService)
    {
        _engagementService = engagementService;
    }

    public IEnumerable<VoucherViewModel> Vouchers { get; set; } = new List<VoucherViewModel>();

    public async Task OnGetAsync()
    {
        try
        {
            var token = User.FindFirst(CoffeeShop.WebRazorPages.Constants.AuthConstants.AccessTokenClaimType)?.Value
                     ?? HttpContext.Session.GetString(CoffeeShop.WebRazorPages.Constants.AuthConstants.JwtSessionKey)
                     ?? Request.Cookies["access_token"];
            var res = await _engagementService.GetAllVouchersForAdminAsync(token);
            if (res.IsSuccess && res.Data != null)
            {
                Vouchers = res.Data;
            }
        }
        catch
        {
            // Will load via JS on client
        }
    }
}
