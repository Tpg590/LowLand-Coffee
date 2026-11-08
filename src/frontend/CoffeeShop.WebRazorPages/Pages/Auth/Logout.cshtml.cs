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
                              ?? HttpContext.Session.GetString(AuthConstants.JwtSessionKey)
                              ?? Request.Cookies["access_token"];

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                await _authHttpService.LogoutAsync(accessToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi gọi API đăng xuất qua Gateway.");
        }

        // 1. Xóa Cookie Authentication phía Server
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // 2. Xóa toàn bộ Session lưu trữ JWT Token
        HttpContext.Session.Remove(AuthConstants.JwtSessionKey);
        HttpContext.Session.Clear();

        // 3. Xóa triệt để toàn bộ Cookie liên quan đến Token và Phiên đăng nhập
        var deleteCookieOptions = new CookieOptions
        {
            Path = "/",
            HttpOnly = false,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UnixEpoch
        };
        Response.Cookies.Delete("access_token", deleteCookieOptions);
        Response.Cookies.Delete("refresh_token", deleteCookieOptions);
        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("refresh_token");
        Response.Cookies.Delete(".AspNetCore.Cookies");
        Response.Cookies.Delete(".AspNetCore.Session");

        TempData["LoggedOut"] = "true";
        TempData["StatusMessage"] = "Bạn đã đăng xuất khỏi hệ thống thành công.";
        return RedirectToPage(AuthConstants.LoginRoute);
    }
}

