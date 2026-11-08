using CoffeeShop.WebRazorPages.ViewModels.Common;
using CoffeeShop.WebRazorPages.ViewModels.Order;

namespace CoffeeShop.WebRazorPages.Services;

public interface IOrderHttpService
{
    Task<ApiResponse<OrderDetailViewModel>> CreateOrderAsync(CreateOrderInputViewModel request, string? accessToken = null);
    Task<ApiResponse<PagedResult<OrderDtoViewModel>>> GetMyOrdersAsync(OrderFilterViewModel filter, string? accessToken = null);
    Task<ApiResponse<PagedResult<OrderDtoViewModel>>> GetAllOrdersAsync(OrderFilterViewModel filter, string? accessToken = null);
    Task<ApiResponse<OrderDetailViewModel>> GetOrderByIdAsync(Guid orderId, string? accessToken = null);
    Task<ApiResponse<OrderDetailViewModel>> UpdateOrderStatusAsync(Guid orderId, string status, string? note, string? accessToken = null);
    Task<ApiResponse<OrderDetailViewModel>> CancelOrderAsync(Guid orderId, string reason, string? accessToken = null);
    Task<ApiResponse<VnPayPaymentResponseViewModel>> GenerateVnPayUrlAsync(Guid orderId, string? returnUrl, string? accessToken = null);
    Task<ApiResponse<VnPayReturnResultViewModel>> ProcessVnPayReturnAsync(string rawQueryString);
    Task<ApiResponse<VnPayReturnResultViewModel>> SimulateVnPayPaymentAsync(Guid orderId, string? accessToken = null);
    Task<ApiResponse<OrderPaymentViewModel>> ConfirmCashPaymentAsync(Guid orderId, string? transactionCode, string? note, string? accessToken = null);
    Task<List<PosProductItemViewModel>> GetPosProductsAsync();
    Task<List<PosCategoryItemViewModel>> GetPosCategoriesAsync();
}

