using CoffeeShop.WebRazorPages.Constants;
using CoffeeShop.WebRazorPages.Services;
using CoffeeShop.WebRazorPages.ViewModels.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoffeeShop.WebRazorPages.Pages.Auth;

public class ForgotPasswordModel : PageModel
{
    private readonly IAuthHttpService _authHttpService;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(IAuthHttpService authHttpService, ILogger<ForgotPasswordModel> logger)
    {
        _authHttpService = authHttpService;
        _logger = logger;
    }

    [BindProperty]
    public ForgotPasswordViewModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage(AuthConstants.IndexRoute);
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
            var result = await _authHttpService.ForgotPasswordAsync(Input);

            if (!result.IsSuccess)
            {
                ErrorMessage = !string.IsNullOrEmpty(result.Message)
                    ? result.Message
                    : "Không thể gửi mã xác nhận. Vui lòng kiểm tra lại email.";
                ModelState.AddModelError(string.Empty, ErrorMessage);
                return Page();
            }

            TempData["SuccessToast"] = "Mã xác nhận OTP đã được gửi đến email của bạn.";
            return RedirectToPage(AuthConstants.ResetPasswordRoute, new { email = Input.Email });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý quên mật khẩu cho email {Email}", Input.Email);
            ErrorMessage = "Đã xảy ra lỗi trong quá trình gửi yêu cầu. Vui lòng thử lại.";
            ModelState.AddModelError(string.Empty, ErrorMessage);
            return Page();
        }
    }
}

