using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoffeeShop.WebRazorPages.Pages.Staff;

public class OrderListModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Staff/Orders", new { view = "orders" });
    }
}
