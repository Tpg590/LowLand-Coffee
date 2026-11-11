using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoffeeShop.WebRazorPages.Pages.Auth;

public class AccessDeniedModel : PageModel
{
    public string ReturnTargetUrl { get; private set; } = "/";

    public void OnGet()
    {
        if (User.IsInRole("Admin"))
        {
            ReturnTargetUrl = "/Admin/Users";
        }
        else if (User.IsInRole("Staff"))
        {
            ReturnTargetUrl = "/Staff/Orders";
        }
        else
        {
            ReturnTargetUrl = "/";
        }
    }
}

