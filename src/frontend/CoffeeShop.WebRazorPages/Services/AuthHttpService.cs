using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CoffeeShop.WebRazorPages.Constants;
using CoffeeShop.WebRazorPages.ViewModels.Auth;
using CoffeeShop.WebRazorPages.ViewModels.Common;

namespace CoffeeShop.WebRazorPages.Services;

/// <summary>
/// Triển khai Service HTTP nội bộ cho feature Auth
/// </summary>
public class AuthHttpService : IAuthHttpService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthHttpService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuthHttpService(HttpClient httpClient, ILogger<AuthHttpService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResponseViewModel>> LoginAsync(LoginViewModel request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(AuthConstants.LoginEndpoint, request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<AuthResponseViewModel>>(content, JsonOptions);
            return result ?? ApiResponse<AuthResponseViewModel>.Failure("Không thể đọc dữ liệu phản hồi từ máy chủ.", (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối khi gửi yêu cầu đăng nhập tới API Gateway.");
            return ApiResponse<AuthResponseViewModel>.Failure("Không thể kết nối đến máy chủ API Gateway. Vui lòng thử lại sau.", 503);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định khi đăng nhập.");
            return ApiResponse<AuthResponseViewModel>.Failure("Đã xảy ra lỗi hệ thống khi xử lý đăng nhập.", 500);
        }
    }

    public async Task<ApiResponse<AuthResponseViewModel>> RegisterAsync(RegisterViewModel request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(AuthConstants.RegisterEndpoint, request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<AuthResponseViewModel>>(content, JsonOptions);
            return result ?? ApiResponse<AuthResponseViewModel>.Failure("Không thể đọc dữ liệu phản hồi từ máy chủ.", (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối khi gửi yêu cầu đăng ký tới API Gateway.");
            return ApiResponse<AuthResponseViewModel>.Failure("Không thể kết nối đến máy chủ API Gateway. Vui lòng thử lại sau.", 503);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định khi đăng ký.");
            return ApiResponse<AuthResponseViewModel>.Failure("Đã xảy ra lỗi hệ thống khi xử lý đăng ký.", 500);
        }
    }

    public async Task<ApiResponse<AuthResponseViewModel>> RefreshTokenAsync(RefreshTokenViewModel request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(AuthConstants.RefreshTokenEndpoint, request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<AuthResponseViewModel>>(content, JsonOptions);
            return result ?? ApiResponse<AuthResponseViewModel>.Failure("Không thể đọc phản hồi từ máy chủ.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi RefreshTokenAsync qua Gateway.");
            return ApiResponse<AuthResponseViewModel>.Failure("Lỗi khi cấp lại Access Token.", 500);
        }
    }

    public async Task<ApiResponse<UserViewModel>> GetProfileAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, AuthConstants.ProfileEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<UserViewModel>>(content, JsonOptions);
            return result ?? ApiResponse<UserViewModel>.Failure("Không thể đọc phản hồi từ máy chủ.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin Profile qua Gateway.");
            return ApiResponse<UserViewModel>.Failure("Lỗi khi truy xuất hồ sơ cá nhân.", 500);
        }
    }

    public async Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordViewModel request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(AuthConstants.ForgotPasswordEndpoint, request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<string>>(content, JsonOptions);
            return result ?? ApiResponse<string>.Failure("Không thể đọc phản hồi từ máy chủ.", (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối khi gửi yêu cầu quên mật khẩu tới API Gateway.");
            return ApiResponse<string>.Failure("Không thể kết nối đến máy chủ API Gateway.", 503);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi ForgotPasswordAsync.");
            return ApiResponse<string>.Failure("Đã xảy ra lỗi khi yêu cầu đặt lại mật khẩu.", 500);
        }
    }

    public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordViewModel request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(AuthConstants.ResetPasswordEndpoint, request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<bool>>(content, JsonOptions);
            return result ?? ApiResponse<bool>.Failure("Không thể đọc phản hồi từ máy chủ.", (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối khi gửi yêu cầu đặt lại mật khẩu tới API Gateway.");
            return ApiResponse<bool>.Failure("Không thể kết nối đến máy chủ API Gateway.", 503);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi ResetPasswordAsync.");
            return ApiResponse<bool>.Failure("Đã xảy ra lỗi khi xác thực đặt lại mật khẩu.", 500);
        }
    }

    public async Task<ApiResponse<UserViewModel>> UpdateProfileAsync(string accessToken, UpdateProfileViewModel request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Put, AuthConstants.ProfileEndpoint)
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<UserViewModel>>(content, JsonOptions);
            return result ?? ApiResponse<UserViewModel>.Failure("Không thể đọc phản hồi từ máy chủ.", (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối khi cập nhật hồ sơ tới API Gateway.");
            return ApiResponse<UserViewModel>.Failure("Không thể kết nối đến máy chủ API Gateway.", 503);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi UpdateProfileAsync.");
            return ApiResponse<UserViewModel>.Failure("Đã xảy ra lỗi khi cập nhật thông tin cá nhân.", 500);
        }
    }

    public async Task<ApiResponse<bool>> ChangePasswordAsync(string accessToken, ChangePasswordViewModel request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, AuthConstants.ChangePasswordEndpoint)
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<bool>>(content, JsonOptions);
            return result ?? ApiResponse<bool>.Failure("Không thể đọc phản hồi từ máy chủ.", (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối khi đổi mật khẩu tới API Gateway.");
            return ApiResponse<bool>.Failure("Không thể kết nối đến máy chủ API Gateway.", 503);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi ChangePasswordAsync.");
            return ApiResponse<bool>.Failure("Đã xảy ra lỗi khi đổi mật khẩu.", 500);
        }
    }

    public async Task<ApiResponse<UserViewModel>> UploadAvatarAsync(string accessToken, Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            using var formData = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var mime = ext switch
            {
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                _ => "image/jpeg"
            };
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(mime);
            formData.Add(streamContent, "File", fileName);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, AuthConstants.AvatarUploadEndpoint)
            {
                Content = formData
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<UserViewModel>>(content, JsonOptions);
            return result ?? ApiResponse<UserViewModel>.Failure("Không thể đọc phản hồi từ máy chủ.", (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối khi tải lên ảnh đại diện tới API Gateway.");
            return ApiResponse<UserViewModel>.Failure("Không thể kết nối đến máy chủ API Gateway.", 503);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tải lên ảnh đại diện.");
            return ApiResponse<UserViewModel>.Failure("Đã xảy ra lỗi khi tải lên ảnh đại diện.", 500);
        }
    }

    public async Task<ApiResponse<bool>> LogoutAsync(string? accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            return ApiResponse<bool>.Success(true, "Đăng xuất thành công");
        }

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, AuthConstants.LogoutEndpoint);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<bool>>(content, JsonOptions);
            return result ?? ApiResponse<bool>.Success(true, "Đăng xuất thành công");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi thu hồi token phía backend khi đăng xuất, tiếp tục dọn dẹp cookie.");
            return ApiResponse<bool>.Success(true, "Đăng xuất cục bộ thành công");
        }
    }
}

