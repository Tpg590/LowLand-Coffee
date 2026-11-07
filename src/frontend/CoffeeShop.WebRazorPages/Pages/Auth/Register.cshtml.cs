using System.Text.Json;
using CoffeeShop.WebRazorPages.Constants;
using CoffeeShop.WebRazorPages.Services;
using CoffeeShop.WebRazorPages.ViewModels.Auth;
using CoffeeShop.WebRazorPages.ViewModels.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace CoffeeShop.WebRazorPages.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly IAuthHttpService _authHttpService;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        IAuthHttpService authHttpService,
        ILogger<RegisterModel> logger)
    {
        _authHttpService = authHttpService;
        _logger = logger;
    }

    [BindProperty]
    public RegisterViewModel Input { get; set; } = new();

    /// <summary>
    /// UserViewModel trả về từ API sau khi đăng ký (ném trực tiếp ra UI)
    /// </summary>
    public UserViewModel? CreatedUser { get; private set; }

    public string? ErrorMessage { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? RegisteredEmail { get; set; }

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
        if (!Input.AcceptTerms)
        {
            ModelState.AddModelError("Input.AcceptTerms", "Bạn phải đồng ý với Điều khoản dịch vụ.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(Input.PhoneNumber))
            {
                Input.PhoneNumber = Regex.Replace(Input.PhoneNumber.Trim(), @"[\s.-]", "");
            }

            var apiResult = await _authHttpService.RegisterAsync(Input);

            if (!apiResult.IsSuccess)
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
                    : (!string.IsNullOrEmpty(detailedError) ? detailedError : "Email hoặc số điện thoại đã tồn tại trong hệ thống.");

                ModelState.AddModelError(string.Empty, ErrorMessage);
                return Page();
            }

            CreatedUser = apiResult.Data!.User!;
            _logger.LogInformation("Người dùng {Email} đăng ký tài khoản thành công.", Input.Email);

            StatusMessage = $"Đăng ký tài khoản thành công! Xin chào {CreatedUser.FullName}, vui lòng đăng nhập.";
            RegisteredEmail = CreatedUser.Email;

            return RedirectToPage(AuthConstants.LoginRoute);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định khi đăng ký.");
            ErrorMessage = "Đã xảy ra lỗi trong quá trình đăng ký. Vui lòng thử lại.";
            ModelState.AddModelError(string.Empty, ErrorMessage);
            return Page();
        }
    }
}
