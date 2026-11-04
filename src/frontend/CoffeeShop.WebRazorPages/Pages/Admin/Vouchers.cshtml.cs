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
            var res = await _engagementService.GetAllVouchersForAdminAsync();
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
