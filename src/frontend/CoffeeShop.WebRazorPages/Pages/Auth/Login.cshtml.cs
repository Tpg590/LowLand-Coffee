using System.Security.Claims;
using System.Text.Json;
using CoffeeShop.WebRazorPages.Constants;
using CoffeeShop.WebRazorPages.Services;
using CoffeeShop.WebRazorPages.ViewModels.Auth;
using CoffeeShop.WebRazorPages.ViewModels.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoffeeShop.WebRazorPages.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly IAuthHttpService _authHttpService;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        IAuthHttpService authHttpService,
        ILogger<LoginModel> logger)
    {
        _authHttpService = authHttpService;
        _logger = logger;
    }

    [BindProperty]
    public LoginViewModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// UserViewModel trả về từ API sau khi xác thực thành công (ném trực tiếp ra UI)
    /// </summary>
    public UserViewModel? AuthenticatedUser { get; private set; }

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectUserByRole(ReturnUrl);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var apiResult = await _authHttpService.LoginAsync(Input);

            if (!apiResult.IsSuccess || apiResult.Data == null || string.IsNullOrEmpty(apiResult.Data.AccessToken))
            {
                string? detailedError = null;
                if (apiResult.Errors is IEnumerable<string> errList)
                {
                    detailedError = errList.FirstOrDefault();
                }
                else if (apiResult.Errors is JsonElement jsonElem && jsonElem.ValueKind == JsonValueKind.Array)
                {
                    detailedError = jsonElem.EnumerateArray().FirstOrDefault().GetString();
                }
                else if (apiResult.Errors != null)
                {
                    detailedError = apiResult.Errors.ToString();
                }

                ErrorMessage = !string.IsNullOrEmpty(apiResult.Message)
                    ? apiResult.Message
                    : (!string.IsNullOrEmpty(detailedError) ? detailedError : "Email/Số điện thoại hoặc mật khẩu không chính xác.");

                ModelState.AddModelError(string.Empty, ErrorMessage);
                return Page();
            }

            var authData = apiResult.Data;
            AuthenticatedUser = authData.User!;
            var role = !string.IsNullOrEmpty(AuthenticatedUser.RoleName) ? AuthenticatedUser.RoleName : AuthenticatedUser.Role!;

            // 1. Cấp Cookie Authentication phía Server từ UserViewModel
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, AuthenticatedUser.Id.ToString()),
                new(ClaimTypes.Name, AuthenticatedUser.FullName),
                new(ClaimTypes.Email, AuthenticatedUser.Email),
                new(ClaimTypes.Role, role),
                new(AuthConstants.AccessTokenClaimType, authData.AccessToken)
            };

            if (!string.IsNullOrEmpty(AuthenticatedUser.PhoneNumber))
            {
                claims.Add(new Claim(ClaimTypes.MobilePhone, AuthenticatedUser.PhoneNumber));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // 2. Lưu token vào Server Session
            HttpContext.Session.SetString(AuthConstants.JwtSessionKey, authData.AccessToken);

            // 3. Set Cookie đồng bộ cho client
            Response.Cookies.Append("access_token", authData.AccessToken, new CookieOptions
            {
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(24)
            });

            _logger.LogInformation("Người dùng {Email} đăng nhập thành công với vai trò {Role}.", AuthenticatedUser.Email, role);

            // 4. Server-side Redirect chuẩn PRG
            return RedirectUserByRole(ReturnUrl, role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định khi đăng nhập.");
            ErrorMessage = "Đã xảy ra lỗi trong quá trình xử lý đăng nhập. Vui lòng thử lại.";
            ModelState.AddModelError(string.Empty, ErrorMessage);
            return Page();
        }
    }

    private IActionResult RedirectUserByRole(string? returnUrl, string? explicitRole = null)
    {
        var role = explicitRole ?? User.Claims.First(c => c.Type == ClaimTypes.Role).Value;

        if (string.Equals(role, AuthConstants.RoleAdmin, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToPage(AuthConstants.AdminUsersRoute);
        }

        if (string.Equals(role, AuthConstants.RoleStaff, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl.StartsWith("/staff", StringComparison.OrdinalIgnoreCase))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToPage(AuthConstants.StaffOrdersRoute);
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) 
            && !returnUrl.StartsWith("/admin", StringComparison.OrdinalIgnoreCase) 
            && !returnUrl.StartsWith("/staff", StringComparison.OrdinalIgnoreCase))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToPage(AuthConstants.IndexRoute);
    }
}
