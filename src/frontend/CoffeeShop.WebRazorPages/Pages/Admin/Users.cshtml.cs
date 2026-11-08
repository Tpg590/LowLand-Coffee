using CoffeeShop.WebRazorPages.Constants;
using CoffeeShop.WebRazorPages.Services;
using CoffeeShop.WebRazorPages.ViewModels.Auth;
using CoffeeShop.WebRazorPages.ViewModels.Common;
using CoffeeShop.WebRazorPages.ViewModels.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoffeeShop.WebRazorPages.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
    private readonly IUserHttpService _userHttpService;
    private readonly ILogger<UsersModel> _logger;

    public UsersModel(IUserHttpService userHttpService, ILogger<UsersModel> logger)
    {
        _userHttpService = userHttpService;
        _logger = logger;
    }

    public PagedResult<UserViewModel> Users { get; set; } = new();
    public IEnumerable<RoleViewModel> Roles { get; set; } = Enumerable.Empty<RoleViewModel>();

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? RoleName { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool? IsActive { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    [BindProperty]
    public CreateStaffViewModel CreateStaffInput { get; set; } = new();

    [BindProperty]
    public UpdateUserViewModel UpdateUserInput { get; set; } = new();

    [BindProperty]
    public Guid EditUserId { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadUsersAndRolesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateStaffAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage(AuthConstants.LoginRoute);
        }

        ModelState.Clear();
        TryValidateModel(CreateStaffInput, nameof(CreateStaffInput));

        if (!ModelState.IsValid)
        {
            await LoadUsersAndRolesAsync();
            ErrorMessage = "Dữ liệu cấp tài khoản nhân viên không hợp lệ. Vui lòng kiểm tra lại.";
            return Page();
        }

        var result = await _userHttpService.CreateStaffAsync(token, CreateStaffInput);
        if (!result.IsSuccess)
        {
            await LoadUsersAndRolesAsync();
            ErrorMessage = result.Message ?? "Không thể tạo tài khoản nhân viên. Vui lòng thử lại.";
            return Page();
        }

        SuccessMessage = $"Cấp tài khoản cho nhân viên {CreateStaffInput.FullName} thành công! (Mật khẩu: {CreateStaffInput.Password})";
        return RedirectToPage(new { Search, RoleName, IsActive, PageNumber });
    }

    public async Task<IActionResult> OnPostUpdateUserAsync(Guid? id)
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage(AuthConstants.LoginRoute);
        }

        var targetId = (id.HasValue && id.Value != Guid.Empty) ? id.Value : EditUserId;
        if (targetId == Guid.Empty)
        {
            await LoadUsersAndRolesAsync();
            ErrorMessage = "Không xác định được tài khoản người dùng cần cập nhật.";
            return Page();
        }

        ModelState.Clear();
        TryValidateModel(UpdateUserInput, nameof(UpdateUserInput));

        if (!ModelState.IsValid)
        {
            await LoadUsersAndRolesAsync();
            ErrorMessage = "Dữ liệu cập nhật tài khoản không hợp lệ.";
            return Page();
        }

        var result = await _userHttpService.UpdateUserAsync(token, targetId, UpdateUserInput);
        if (!result.IsSuccess)
        {
            await LoadUsersAndRolesAsync();
            ErrorMessage = result.Message ?? "Không thể cập nhật thông tin tài khoản.";
            return Page();
        }

        SuccessMessage = $"Cập nhật thông tin tài khoản {UpdateUserInput.FullName} thành công!";
        return RedirectToPage(new { Search, RoleName, IsActive, PageNumber });
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(Guid id, bool isActive)
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage(AuthConstants.LoginRoute);
        }

        var result = await _userHttpService.UpdateUserStatusAsync(token, id, isActive);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message ?? "Không thể cập nhật trạng thái tài khoản.";
        }
        else
        {
            SuccessMessage = isActive ? "Mở khóa tài khoản thành công!" : "Đã khóa tài khoản người dùng thành công!";
        }

        return RedirectToPage(new { Search, RoleName, IsActive, PageNumber });
    }

    public async Task<IActionResult> OnPostDeleteUserAsync(Guid id)
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToPage(AuthConstants.LoginRoute);
        }

        var result = await _userHttpService.DeleteUserAsync(token, id);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Message ?? "Không thể xóa tài khoản khỏi hệ thống.";
        }
        else
        {
            SuccessMessage = "Đã xóa vĩnh viễn tài khoản người dùng khỏi hệ thống!";
        }

        return RedirectToPage(new { Search, RoleName, IsActive, PageNumber });
    }

    private async Task LoadUsersAndRolesAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrEmpty(token)) return;

        var query = new UserQueryParametersViewModel
        {
            Search = Search,
            RoleName = RoleName,
            IsActive = IsActive,
            PageNumber = PageNumber > 0 ? PageNumber : 1,
            PageSize = PageSize > 0 ? PageSize : 10
        };

        var usersTask = _userHttpService.GetUsersAsync(token, query);
        var rolesTask = _userHttpService.GetRolesAsync(token);

        await Task.WhenAll(usersTask, rolesTask);

        var usersResult = await usersTask;
        if (usersResult.IsSuccess && usersResult.Data != null)
        {
            Users = usersResult.Data;
        }

        var rolesResult = await rolesTask;
        if (rolesResult.IsSuccess && rolesResult.Data != null)
        {
            Roles = rolesResult.Data;
        }
    }

    private string? GetAccessToken()
    {
        return User.FindFirst(AuthConstants.AccessTokenClaimType)?.Value
               ?? HttpContext.Session.GetString(AuthConstants.JwtSessionKey)
               ?? Request.Cookies["access_token"];
    }
}
