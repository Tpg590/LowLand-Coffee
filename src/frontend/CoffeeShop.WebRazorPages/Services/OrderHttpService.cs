using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CoffeeShop.WebRazorPages.ViewModels.Common;
using CoffeeShop.WebRazorPages.ViewModels.Order;

namespace CoffeeShop.WebRazorPages.Services;

public class OrderHttpService : IOrderHttpService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OrderHttpService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OrderHttpService(IHttpClientFactory httpClientFactory, ILogger<OrderHttpService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateClient(string? accessToken = null)
    {
        var client = _httpClientFactory.CreateClient("ApiGateway");
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        return client;
    }

    public async Task<ApiResponse<OrderDetailViewModel>> CreateOrderAsync(CreateOrderInputViewModel request, string? accessToken = null)
    {
        try
        {
            var client = CreateClient(accessToken);
            var response = await client.PostAsJsonAsync("/api/orders", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrderDetailViewModel>>(JsonOptions);
            return result ?? ApiResponse<OrderDetailViewModel>.Failure("Không nhận được phản hồi từ hệ thống Đặt hàng.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API tạo đơn hàng");
            return ApiResponse<OrderDetailViewModel>.Failure("Không thể kết nối tới dịch vụ Đặt hàng.", 503);
        }
    }

    public async Task<ApiResponse<PagedResult<OrderDtoViewModel>>> GetMyOrdersAsync(OrderFilterViewModel filter, string? accessToken = null)
    {
        try
        {
            var client = CreateClient(accessToken);
            var query = BuildFilterQuery(filter);
            var response = await client.GetAsync($"/api/orders/my-orders{query}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<OrderDtoViewModel>>>(JsonOptions);
            return result ?? ApiResponse<PagedResult<OrderDtoViewModel>>.Failure("Không thể tải danh sách đơn hàng của bạn.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API lấy danh sách đơn hàng cá nhân");
            return ApiResponse<PagedResult<OrderDtoViewModel>>.Failure("Không thể kết nối tới dịch vụ Đơn hàng.", 503);
        }
    }

    public async Task<ApiResponse<PagedResult<OrderDtoViewModel>>> GetAllOrdersAsync(OrderFilterViewModel filter, string? accessToken = null)
    {
        try
        {
            var client = CreateClient(accessToken);
            var query = BuildFilterQuery(filter);
            var response = await client.GetAsync($"/api/orders{query}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<OrderDtoViewModel>>>(JsonOptions);
            return result ?? ApiResponse<PagedResult<OrderDtoViewModel>>.Failure("Không thể tải danh sách toàn bộ đơn hàng.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API lấy danh sách đơn hàng cho Staff");
            return ApiResponse<PagedResult<OrderDtoViewModel>>.Failure("Không thể kết nối tới dịch vụ Đơn hàng.", 503);
        }
    }

    public async Task<ApiResponse<OrderDetailViewModel>> GetOrderByIdAsync(Guid orderId, string? accessToken = null)
    {
        try
        {
            var client = CreateClient(accessToken);
            var response = await client.GetAsync($"/api/orders/{orderId}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrderDetailViewModel>>(JsonOptions);
            return result ?? ApiResponse<OrderDetailViewModel>.Failure("Không tìm thấy thông tin chi tiết đơn hàng.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API lấy chi tiết đơn hàng {OrderId}", orderId);
            return ApiResponse<OrderDetailViewModel>.Failure("Không thể kết nối tới dịch vụ Đơn hàng.", 503);
        }
    }

    public async Task<ApiResponse<OrderDetailViewModel>> UpdateOrderStatusAsync(Guid orderId, string status, string? note, string? accessToken = null)
    {
        try
        {
            var client = CreateClient(accessToken);
            var payload = new UpdateOrderStatusInputViewModel
            {
                Status = status,
                Note = note
            };
            var response = await client.PutAsJsonAsync($"/api/orders/{orderId}/status", payload);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrderDetailViewModel>>(JsonOptions);
            return result ?? ApiResponse<OrderDetailViewModel>.Failure("Cập nhật trạng thái đơn hàng thất bại.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật trạng thái đơn hàng {OrderId} -> {Status}", orderId, status);
            return ApiResponse<OrderDetailViewModel>.Failure("Không thể kết nối tới dịch vụ Đơn hàng.", 503);
        }
    }

    public async Task<ApiResponse<OrderDetailViewModel>> CancelOrderAsync(Guid orderId, string reason, string? accessToken = null)
    {
        try
        {
            var client = CreateClient(accessToken);
            var payload = new CancelOrderInputViewModel { Reason = reason };
            var response = await client.PostAsJsonAsync($"/api/orders/{orderId}/cancel", payload);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrderDetailViewModel>>(JsonOptions);
            return result ?? ApiResponse<OrderDetailViewModel>.Failure("Hủy đơn hàng thất bại.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi hủy đơn hàng {OrderId}", orderId);
            return ApiResponse<OrderDetailViewModel>.Failure("Không thể kết nối tới dịch vụ Đơn hàng.", 503);
        }
    }

    public async Task<ApiResponse<VnPayPaymentResponseViewModel>> GenerateVnPayUrlAsync(Guid orderId, string? returnUrl, string? accessToken = null)
    {
        try
        {
            var client = CreateClient(accessToken);
            var payload = new { orderId, returnUrl };
            var response = await client.PostAsJsonAsync("/api/payments/vnpay/create-url", payload);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<VnPayPaymentResponseViewModel>>(JsonOptions);
            return result ?? ApiResponse<VnPayPaymentResponseViewModel>.Failure("Không thể khởi tạo mã QR VNPay.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo mã QR VNPay cho đơn {OrderId}", orderId);
            return ApiResponse<VnPayPaymentResponseViewModel>.Failure("Không thể kết nối tới dịch vụ Thanh toán VNPay.", 503);
        }
    }

    public async Task<ApiResponse<VnPayReturnResultViewModel>> ProcessVnPayReturnAsync(string rawQueryString)
    {
        try
        {
            var client = CreateClient();
            var qs = rawQueryString.StartsWith('?') ? rawQueryString : $"?{rawQueryString}";
            var response = await client.GetAsync($"/api/payments/vnpay-return{qs}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<VnPayReturnResultViewModel>>(JsonOptions);
            return result ?? ApiResponse<VnPayReturnResultViewModel>.Failure("Không thể đồng bộ kết quả thanh toán VNPay.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đồng bộ kết quả trả về từ VNPay");
            return ApiResponse<VnPayReturnResultViewModel>.Failure("Không thể kết nối tới dịch vụ Thanh toán VNPay.", 503);
        }
    }

    public async Task<ApiResponse<VnPayReturnResultViewModel>> SimulateVnPayPaymentAsync(Guid orderId, string? accessToken = null)
    {
        try
        {
            var client = CreateClient(accessToken);
            var response = await client.PostAsync($"/api/payments/{orderId}/vnpay-simulate", null);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<VnPayReturnResultViewModel>>(JsonOptions);
            return result ?? ApiResponse<VnPayReturnResultViewModel>.Failure("Không thể xác nhận thanh toán VNPay Sandbox.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi giả lập thanh toán VNPay cho đơn {OrderId}", orderId);
            return ApiResponse<VnPayReturnResultViewModel>.Failure("Không thể kết nối tới dịch vụ Thanh toán VNPay.", 503);
        }
    }

    public async Task<ApiResponse<OrderPaymentViewModel>> ConfirmCashPaymentAsync(Guid orderId, string? transactionCode, string? note, string? accessToken = null)
    {
        try
        {
            var client = CreateClient(accessToken);
            var payload = new CashPaymentConfirmInputViewModel
            {
                TransactionCode = transactionCode,
                Note = note
            };
            var response = await client.PutAsJsonAsync($"/api/payments/{orderId}/cash-confirm", payload);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrderPaymentViewModel>>(JsonOptions);
            return result ?? ApiResponse<OrderPaymentViewModel>.Failure("Xác nhận thu tiền mặt thất bại.", (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xác nhận thu tiền mặt cho đơn {OrderId}", orderId);
            return ApiResponse<OrderPaymentViewModel>.Failure("Không thể kết nối tới dịch vụ Thanh toán.", 503);
        }
    }

    public async Task<List<PosProductItemViewModel>> GetPosProductsAsync()
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetFromJsonAsync<ApiResponse<PagedResult<PosProductItemViewModel>>>("/api/products?pageSize=100", JsonOptions);
            return response?.Data?.Items?.ToList() ?? new List<PosProductItemViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể tải danh sách sản phẩm cho quầy POS");
            return new List<PosProductItemViewModel>();
        }
    }

    public async Task<List<PosCategoryItemViewModel>> GetPosCategoriesAsync()
    {
        try
        {
            var client = CreateClient();
            var response = await client.GetFromJsonAsync<ApiResponse<List<PosCategoryItemViewModel>>>("/api/categories", JsonOptions);
            return response?.Data?.Where(c => c.IsActive).ToList() ?? new List<PosCategoryItemViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể tải danh sách danh mục cho quầy POS");
            return new List<PosCategoryItemViewModel>();
        }
    }

    private static string BuildFilterQuery(OrderFilterViewModel filter)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={(filter.PageNumber > 0 ? filter.PageNumber : 1)}",
            $"pageSize={(filter.PageSize > 0 ? filter.PageSize : 50)}"
        };

        if (!string.IsNullOrWhiteSpace(filter.Status) && !string.Equals(filter.Status, "ALL", StringComparison.OrdinalIgnoreCase))
        {
            queryParams.Add($"status={Uri.EscapeDataString(filter.Status.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(filter.OrderCode))
        {
            queryParams.Add($"orderCode={Uri.EscapeDataString(filter.OrderCode.Trim())}");
        }

        if (filter.FromDate.HasValue)
        {
            queryParams.Add($"fromDate={Uri.EscapeDataString(filter.FromDate.Value.ToString("yyyy-MM-dd"))}");
        }

        if (filter.ToDate.HasValue)
        {
            queryParams.Add($"toDate={Uri.EscapeDataString(filter.ToDate.Value.ToString("yyyy-MM-dd"))}");
        }

        return queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
    }
}

