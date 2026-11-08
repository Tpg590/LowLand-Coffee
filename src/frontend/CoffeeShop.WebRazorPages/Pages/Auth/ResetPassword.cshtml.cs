using CoffeeShop.WebRazorPages.Constants;
using CoffeeShop.WebRazorPages.Services;
using CoffeeShop.WebRazorPages.ViewModels.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoffeeShop.WebRazorPages.Pages.Auth;

public class ResetPasswordModel : PageModel
{
    private readonly IAuthHttpService _authHttpService;
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(IAuthHttpService authHttpService, ILogger<ResetPasswordModel> logger)
    {
        _authHttpService = authHttpService;
        _logger = logger;
    }

    [BindProperty]
    public ResetPasswordViewModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Email { get; set; }

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage(AuthConstants.IndexRoute);
        }

        if (!string.IsNullOrEmpty(Email))
        {
            Input.Email = Email;
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
            var result = await _authHttpService.ResetPasswordAsync(Input);

            if (!result.IsSuccess)
            {
                ErrorMessage = !string.IsNullOrEmpty(result.Message)
                    ? result.Message
                    : "Mã OTP không chính xác hoặc đã hết hạn.";
                ModelState.AddModelError(string.Empty, ErrorMessage);
                return Page();
            }

            TempData["StatusMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập với mật khẩu mới.";
            TempData["SuccessToast"] = "Cập nhật mật khẩu thành công!";
            TempData["RegisteredEmail"] = Input.Email;

            return RedirectToPage(AuthConstants.LoginRoute);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đặt lại mật khẩu cho {Email}", Input.Email);
            ErrorMessage = "Đã xảy ra lỗi trong quá trình đặt lại mật khẩu. Vui lòng thử lại.";
            ModelState.AddModelError(string.Empty, ErrorMessage);
            return Page();
        }
    }
}

