using CoffeeShop.WebRazorPages.Constants;
using CoffeeShop.WebRazorPages.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoffeeShop.WebRazorPages.Pages.Auth;

public class LogoutModel : PageModel
{
    private readonly IAuthHttpService _authHttpService;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(IAuthHttpService authHttpService, ILogger<LogoutModel> logger)
    {
        _authHttpService = authHttpService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        return await ProcessLogoutAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return await ProcessLogoutAsync();
    }

    private async Task<IActionResult> ProcessLogoutAsync()
    {
        try
        {
            var accessToken = User.FindFirst(AuthConstants.AccessTokenClaimType)?.Value
                              ?? HttpContext.Session.GetString(AuthConstants.JwtSessionKey);

            if (!string.IsNullOrEmpty(accessToken))
            {
                await _authHttpService.LogoutAsync(accessToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi gọi API đăng xuất qua Gateway.");
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        Response.Cookies.Delete("access_token");

        TempData["StatusMessage"] = "Bạn đã đăng xuất khỏi hệ thống thành công.";
        return RedirectToPage(AuthConstants.LoginRoute);
    }
}

