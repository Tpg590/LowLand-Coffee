using CoffeeShop.WebRazorPages.ViewModels.Auth;
using CoffeeShop.WebRazorPages.ViewModels.Common;

namespace CoffeeShop.WebRazorPages.Services;

/// <summary>
/// Service HTTP giao tiếp với Microservice Auth thông qua API Gateway
/// </summary>
public interface IAuthHttpService
{
    /// <summary>
    /// Gửi request đăng nhập hệ thống, trả về ViewModel
    /// </summary>
    Task<ApiResponse<AuthResponseViewModel>> LoginAsync(LoginViewModel request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi request đăng ký tài khoản mới, trả về ViewModel
    /// </summary>
    Task<ApiResponse<AuthResponseViewModel>> RegisterAsync(RegisterViewModel request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cấp phát lại Access Token mới từ Refresh Token
    /// </summary>
    Task<ApiResponse<AuthResponseViewModel>> RefreshTokenAsync(RefreshTokenViewModel request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy thông tin hồ sơ tài khoản hiện tại từ JWT token
    /// </summary>
    Task<ApiResponse<UserViewModel>> GetProfileAsync(string accessToken, CancellationToken cancellationToken = default);
}

