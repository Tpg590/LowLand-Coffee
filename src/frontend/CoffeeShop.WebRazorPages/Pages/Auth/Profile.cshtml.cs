using System.Security.Claims;
using CoffeeShop.WebRazorPages.Constants;
using CoffeeShop.WebRazorPages.Services;
using CoffeeShop.WebRazorPages.ViewModels.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoffeeShop.WebRazorPages.Pages.Auth;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly IAuthHttpService _authHttpService;
    private readonly ILogger<ProfileModel> _logger;

    public ProfileModel(IAuthHttpService authHttpService, ILogger<ProfileModel> logger)
    {
        _authHttpService = authHttpService;
        _logger = logger;
    }

    public UserViewModel UserProfile { get; set; } = new();

    [BindProperty]
    public UpdateProfileViewModel ProfileInput { get; set; } = new();

    [BindProperty]
    public ChangePasswordViewModel PasswordInput { get; set; } = new();

    [BindProperty]
    public IFormFile? AvatarFile { get; set; }

    [TempData]
    public string? ToastMessage { get; set; }

    [TempData]
    public string? ToastType { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage(AuthConstants.LoginRoute, new { returnUrl = "/Auth/Profile" });
        }

        var profileResult = await _authHttpService.GetProfileAsync(token);
        if (!profileResult.IsSuccess || profileResult.Data == null)
        {
            ToastMessage = "Không thể tải thông tin hồ sơ cá nhân. Vui lòng đăng nhập lại.";
            ToastType = "error";
            return RedirectToPage(AuthConstants.LoginRoute);
        }

        UserProfile = profileResult.Data;
        ProfileInput = new UpdateProfileViewModel
        {
            FullName = UserProfile.FullName,
            PhoneNumber = UserProfile.PhoneNumber ?? string.Empty,
            Address = UserProfile.Address,
            AvatarUrl = UserProfile.AvatarUrl
        };

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage(AuthConstants.LoginRoute);
        }

        ModelState.Remove("PasswordInput.CurrentPassword");
        ModelState.Remove("PasswordInput.NewPassword");
        ModelState.Remove("PasswordInput.ConfirmPassword");

        if (!ModelState.IsValid)
        {
            await ReloadUserProfileAsync(token);
            ToastMessage = "Dữ liệu cập nhật hồ sơ không hợp lệ.";
            ToastType = "error";
            return Page();
        }

        try
        {
            var updateResult = await _authHttpService.UpdateProfileAsync(token, ProfileInput);
            if (!updateResult.IsSuccess)
            {
                await ReloadUserProfileAsync(token);
                ToastMessage = updateResult.Message ?? "Cập nhật hồ sơ thất bại.";
                ToastType = "error";
                ModelState.AddModelError(string.Empty, ToastMessage);
                return Page();
            }

            // Nếu người dùng chọn tải lên avatar mới
            if (AvatarFile != null && AvatarFile.Length > 0)
            {
                using var stream = AvatarFile.OpenReadStream();
                var avatarResult = await _authHttpService.UploadAvatarAsync(token, stream, AvatarFile.FileName);
                if (!avatarResult.IsSuccess)
                {
                    _logger.LogWarning("Tải ảnh đại diện thất bại: {Msg}", avatarResult.Message);
                }
            }

            ToastMessage = "Cập nhật hồ sơ thành công!";
            ToastType = "success";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật hồ sơ.");
            await ReloadUserProfileAsync(token);
            ToastMessage = "Đã xảy ra lỗi khi lưu thông tin hồ sơ.";
            ToastType = "error";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage(AuthConstants.LoginRoute);
        }

        ModelState.Remove("ProfileInput.FullName");
        ModelState.Remove("ProfileInput.PhoneNumber");

        if (!ModelState.IsValid)
        {
            await ReloadUserProfileAsync(token);
            ToastMessage = "Dữ liệu đổi mật khẩu không hợp lệ.";
            ToastType = "error";
            return Page();
        }

        try
        {
            var result = await _authHttpService.ChangePasswordAsync(token, PasswordInput);
            if (!result.IsSuccess)
            {
                await ReloadUserProfileAsync(token);
                ToastMessage = result.Message ?? "Đổi mật khẩu thất bại. Kiểm tra lại mật khẩu hiện tại.";
                ToastType = "error";
                ModelState.AddModelError(string.Empty, ToastMessage);
                return Page();
            }

            ToastMessage = "Đổi mật khẩu tài khoản thành công!";
            ToastType = "success";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đổi mật khẩu.");
            await ReloadUserProfileAsync(token);
            ToastMessage = "Đã xảy ra lỗi trong quá trình đổi mật khẩu.";
            ToastType = "error";
            return Page();
        }
    }

    private string? GetAccessToken()
    {
        return User.FindFirst(AuthConstants.AccessTokenClaimType)?.Value
               ?? HttpContext.Session.GetString(AuthConstants.JwtSessionKey)
               ?? Request.Cookies["access_token"];
    }

    private async Task ReloadUserProfileAsync(string token)
    {
        var profileResult = await _authHttpService.GetProfileAsync(token);
        if (profileResult.IsSuccess && profileResult.Data != null)
        {
            UserProfile = profileResult.Data;
        }
    }
}

