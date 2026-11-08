using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CoffeeShop.WebRazorPages.ViewModels.Common;
using CoffeeShop.WebRazorPages.ViewModels.Engagement;

namespace CoffeeShop.WebRazorPages.Services;

public class EngagementHttpService : IEngagementHttpService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public EngagementHttpService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("ApiGateway");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string uri, string? token = null, object? body = null)
    {
        var request = new HttpRequestMessage(method, uri);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
        }
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }
        return request;
    }

    private async Task<ApiResponse<T>> SendAsync<T>(HttpRequestMessage request)
    {
        try
        {
            using var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(content))
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                if (apiResponse != null)
                {
                    return apiResponse;
                }
            }

            if (response.IsSuccessStatusCode)
            {
                return ApiResponse<T>.Success(default!, "Thao tác thành công", (int)response.StatusCode);
            }

            return ApiResponse<T>.Failure($"Lỗi máy chủ (HTTP {(int)response.StatusCode})", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.Failure($"Lỗi kết nối API Gateway: {ex.Message}", 500);
        }
    }

    #region Loyalty Service Methods

    public async Task<ApiResponse<IEnumerable<LoyaltyTierViewModel>>> GetTiersAsync()
    {
        var req = CreateRequest(HttpMethod.Get, "/api/loyalty/tiers");
        return await SendAsync<IEnumerable<LoyaltyTierViewModel>>(req);
    }

    public async Task<ApiResponse<LoyaltyMemberProfileViewModel>> GetMemberProfileAsync(string? token = null)
    {
        var req = CreateRequest(HttpMethod.Get, "/api/loyalty/me", token);
        return await SendAsync<LoyaltyMemberProfileViewModel>(req);
    }

    public async Task<ApiResponse<LoyaltyMemberProfileViewModel>> GetMemberProfileByUserIdAsync(Guid userId, string? token = null)
    {
        var req = CreateRequest(HttpMethod.Get, $"/api/loyalty/user/{userId}", token);
        return await SendAsync<LoyaltyMemberProfileViewModel>(req);
    }

    public async Task<ApiResponse<IEnumerable<PointTransactionViewModel>>> GetPointTransactionsAsync(string? token = null, int limit = 20)
    {
        var req = CreateRequest(HttpMethod.Get, $"/api/loyalty/transactions?limit={limit}", token);
        return await SendAsync<IEnumerable<PointTransactionViewModel>>(req);
    }

    public async Task<ApiResponse<IEnumerable<PointTransactionViewModel>>> GetUserPointTransactionsAsync(Guid userId, string? token = null, int limit = 20)
    {
        var req = CreateRequest(HttpMethod.Get, $"/api/loyalty/transactions/{userId}?limit={limit}", token);
        return await SendAsync<IEnumerable<PointTransactionViewModel>>(req);
    }

    public async Task<ApiResponse<AddPointsResponseViewModel>> AddPointsAsync(AddPointsRequestViewModel request, string? token = null)
    {
        var req = CreateRequest(HttpMethod.Post, "/api/loyalty/add-points", token, request);
        return await SendAsync<AddPointsResponseViewModel>(req);
    }

    public async Task<ApiResponse<RedeemPointsResponseViewModel>> RedeemPointsAsync(RedeemPointsRequestViewModel request, string? token = null)
    {
        var req = CreateRequest(HttpMethod.Post, "/api/loyalty/redeem", token, request);
        return await SendAsync<RedeemPointsResponseViewModel>(req);
    }

    #endregion

    #region Voucher Service Methods

    public async Task<ApiResponse<IEnumerable<VoucherViewModel>>> GetActiveVouchersAsync()
    {
        var req = CreateRequest(HttpMethod.Get, "/api/vouchers");
        return await SendAsync<IEnumerable<VoucherViewModel>>(req);
    }

    public async Task<ApiResponse<IEnumerable<VoucherViewModel>>> GetAllVouchersForAdminAsync(string? token = null)
    {
        var req = CreateRequest(HttpMethod.Get, "/api/vouchers/all", token);
        return await SendAsync<IEnumerable<VoucherViewModel>>(req);
    }

    public async Task<ApiResponse<VoucherViewModel>> GetVoucherByIdAsync(Guid id, string? token = null)
    {
        var req = CreateRequest(HttpMethod.Get, $"/api/vouchers/{id}", token);
        return await SendAsync<VoucherViewModel>(req);
    }

    public async Task<ApiResponse<VoucherViewModel>> CreateVoucherAsync(CreateVoucherRequestViewModel request, string? token = null)
    {
        var req = CreateRequest(HttpMethod.Post, "/api/vouchers", token, request);
        return await SendAsync<VoucherViewModel>(req);
    }

    public async Task<ApiResponse<VoucherViewModel>> UpdateVoucherAsync(Guid id, UpdateVoucherRequestViewModel request, string? token = null)
    {
        var req = CreateRequest(HttpMethod.Put, $"/api/vouchers/{id}", token, request);
        return await SendAsync<VoucherViewModel>(req);
    }

    public async Task<ApiResponse<bool>> DeleteVoucherAsync(Guid id, string? token = null)
    {
        var req = CreateRequest(HttpMethod.Delete, $"/api/vouchers/{id}", token);
        return await SendAsync<bool>(req);
    }

    public async Task<ApiResponse<ValidateVoucherResponseViewModel>> ValidateVoucherAsync(ValidateVoucherRequestViewModel request, string? token = null)
    {
        var req = CreateRequest(HttpMethod.Post, "/api/vouchers/validate", token, request);
        return await SendAsync<ValidateVoucherResponseViewModel>(req);
    }

    public async Task<ApiResponse<bool>> ApplyVoucherUsageAsync(ApplyVoucherUsageRequestViewModel request, string? token = null)
    {
        var req = CreateRequest(HttpMethod.Post, "/api/vouchers/apply-usage", token, request);
        return await SendAsync<bool>(req);
    }

    public async Task<ApiResponse<IEnumerable<Guid>>> GetMyUsedVoucherIdsAsync(string? token = null)
    {
        var req = CreateRequest(HttpMethod.Get, "/api/vouchers/my-used-ids", token);
        return await SendAsync<IEnumerable<Guid>>(req);
    }

    public async Task<ApiResponse<IEnumerable<Guid>>> GetUserUsedVoucherIdsAsync(Guid userId, string? token = null)
    {
        var req = CreateRequest(HttpMethod.Get, $"/api/vouchers/used-ids/{userId}", token);
        return await SendAsync<IEnumerable<Guid>>(req);
    }

    #endregion
}
