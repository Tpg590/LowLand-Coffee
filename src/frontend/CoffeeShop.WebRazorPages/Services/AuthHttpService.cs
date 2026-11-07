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
}

