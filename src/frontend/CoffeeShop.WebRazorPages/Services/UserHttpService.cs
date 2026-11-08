using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CoffeeShop.WebRazorPages.Constants;
using CoffeeShop.WebRazorPages.ViewModels.Auth;
using CoffeeShop.WebRazorPages.ViewModels.Common;
using CoffeeShop.WebRazorPages.ViewModels.User;

namespace CoffeeShop.WebRazorPages.Services;

public class UserHttpService : IUserHttpService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserHttpService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public UserHttpService(HttpClient httpClient, ILogger<UserHttpService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<UserViewModel>>> GetUsersAsync(string accessToken, UserQueryParametersViewModel query, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(query.Search))
                queryParams.Add($"Search={Uri.EscapeDataString(query.Search.Trim())}");
            if (!string.IsNullOrWhiteSpace(query.RoleName) && query.RoleName != "ALL")
                queryParams.Add($"RoleName={Uri.EscapeDataString(query.RoleName.Trim())}");
            if (query.IsActive.HasValue)
                queryParams.Add($"IsActive={query.IsActive.Value}");
            queryParams.Add($"PageNumber={query.PageNumber}");
            queryParams.Add($"PageSize={query.PageSize}");

            var queryString = string.Join("&", queryParams);
            var endpoint = $"{AuthConstants.UsersEndpoint}?{queryString}";

            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<PagedResult<UserViewModel>>>(content, JsonOptions);
            return result ?? ApiResponse<PagedResult<UserViewModel>>.Failure("Không thể đọc danh sách người dùng từ máy chủ.", (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối khi lấy danh sách người dùng qua API Gateway.");
            return ApiResponse<PagedResult<UserViewModel>>.Failure("Không thể kết nối đến máy chủ API Gateway.", 503);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định khi lấy danh sách người dùng.");
            return ApiResponse<PagedResult<UserViewModel>>.Failure("Đã xảy ra lỗi hệ thống khi tải danh sách người dùng.", 500);
        }
    }

    public async Task<ApiResponse<UserViewModel>> GetUserByIdAsync(string accessToken, Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{AuthConstants.UsersEndpoint}/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<UserViewModel>>(content, JsonOptions);
            return result ?? ApiResponse<UserViewModel>.Failure("Không thể đọc thông tin người dùng từ máy chủ.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy thông tin người dùng ID: {UserId}", id);
            return ApiResponse<UserViewModel>.Failure("Đã xảy ra lỗi khi tải thông tin người dùng.", 500);
        }
    }

    public async Task<ApiResponse<UserViewModel>> CreateStaffAsync(string accessToken, CreateStaffViewModel request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, AuthConstants.CreateStaffEndpoint)
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<UserViewModel>>(content, JsonOptions);
            return result ?? ApiResponse<UserViewModel>.Failure("Không thể đọc phản hồi tạo nhân viên từ máy chủ.", (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối khi cấp tài khoản nhân viên qua API Gateway.");
            return ApiResponse<UserViewModel>.Failure("Không thể kết nối đến máy chủ API Gateway.", 503);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cấp tài khoản nhân viên.");
            return ApiResponse<UserViewModel>.Failure("Đã xảy ra lỗi khi tạo tài khoản nhân viên.", 500);
        }
    }

    public async Task<ApiResponse<UserViewModel>> UpdateUserAsync(string accessToken, Guid id, UpdateUserViewModel request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"{AuthConstants.UsersEndpoint}/{id}")
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<UserViewModel>>(content, JsonOptions);
            return result ?? ApiResponse<UserViewModel>.Failure("Không thể đọc phản hồi cập nhật người dùng từ máy chủ.", (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối khi cập nhật người dùng ID: {UserId}", id);
            return ApiResponse<UserViewModel>.Failure("Không thể kết nối đến máy chủ API Gateway.", 503);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật người dùng ID: {UserId}", id);
            return ApiResponse<UserViewModel>.Failure("Đã xảy ra lỗi khi cập nhật người dùng.", 500);
        }
    }

    public async Task<ApiResponse<UserViewModel>> UpdateUserStatusAsync(string accessToken, Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new UpdateUserStatusViewModel { IsActive = isActive };
            using var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"{AuthConstants.UsersEndpoint}/{id}/status")
            {
                Content = JsonContent.Create(body)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<UserViewModel>>(content, JsonOptions);
            return result ?? ApiResponse<UserViewModel>.Failure("Không thể đọc phản hồi thay đổi trạng thái từ máy chủ.", (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối khi thay đổi trạng thái người dùng ID: {UserId}", id);
            return ApiResponse<UserViewModel>.Failure("Không thể kết nối đến máy chủ API Gateway.", 503);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thay đổi trạng thái người dùng ID: {UserId}", id);
            return ApiResponse<UserViewModel>.Failure("Đã xảy ra lỗi khi thay đổi trạng thái tài khoản.", 500);
        }
    }

    public async Task<ApiResponse<bool>> DeleteUserAsync(string accessToken, Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"{AuthConstants.UsersEndpoint}/{id}");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<bool>>(content, JsonOptions);
            return result ?? ApiResponse<bool>.Failure("Không thể đọc phản hồi xóa người dùng từ máy chủ.", (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Lỗi kết nối khi xóa người dùng ID: {UserId}", id);
            return ApiResponse<bool>.Failure("Không thể kết nối đến máy chủ API Gateway.", 503);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa người dùng ID: {UserId}", id);
            return ApiResponse<bool>.Failure("Đã xảy ra lỗi khi xóa người dùng.", 500);
        }
    }

    public async Task<ApiResponse<IEnumerable<RoleViewModel>>> GetRolesAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, AuthConstants.UserRolesEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<IEnumerable<RoleViewModel>>>(content, JsonOptions);
            return result ?? ApiResponse<IEnumerable<RoleViewModel>>.Failure("Không thể đọc danh sách vai trò từ máy chủ.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách vai trò.");
            return ApiResponse<IEnumerable<RoleViewModel>>.Failure("Đã xảy ra lỗi khi lấy danh sách vai trò.", 500);
        }
    }

    public async Task<ApiResponse<IEnumerable<UserViewModel>>> SearchCustomersAsync(string query, string? accessToken = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return ApiResponse<IEnumerable<UserViewModel>>.Success(Array.Empty<UserViewModel>());
            }

            var endpoint = $"{AuthConstants.UsersEndpoint}/search?q={Uri.EscapeDataString(query.Trim())}";
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var result = JsonSerializer.Deserialize<ApiResponse<IEnumerable<UserViewModel>>>(content, JsonOptions);
            return result ?? ApiResponse<IEnumerable<UserViewModel>>.Failure("Không thể đọc kết quả tìm kiếm khách hàng từ máy chủ.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tìm kiếm khách hàng theo từ khóa: {Query}", query);
            return ApiResponse<IEnumerable<UserViewModel>>.Failure("Đã xảy ra lỗi khi tra cứu khách hàng.", 500);
        }
    }
}

