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

    /// <summary>
    /// Gửi mã OTP xác nhận khôi phục mật khẩu qua Email
    /// </summary>
    Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordViewModel request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xác thực mã OTP và cập nhật mật khẩu mới
    /// </summary>
    Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordViewModel request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật thông tin hồ sơ cá nhân
    /// </summary>
    Task<ApiResponse<UserViewModel>> UpdateProfileAsync(string accessToken, UpdateProfileViewModel request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đổi mật khẩu tài khoản
    /// </summary>
    Task<ApiResponse<bool>> ChangePasswordAsync(string accessToken, ChangePasswordViewModel request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tải lên ảnh đại diện cá nhân
    /// </summary>
    Task<ApiResponse<UserViewModel>> UploadAvatarAsync(string accessToken, Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đăng xuất tài khoản phía Auth Service
    /// </summary>
    Task<ApiResponse<bool>> LogoutAsync(string? accessToken, CancellationToken cancellationToken = default);
}

