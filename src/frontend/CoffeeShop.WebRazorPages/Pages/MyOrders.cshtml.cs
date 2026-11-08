using System.Security.Claims;
using CoffeeShop.WebRazorPages.Constants;
using CoffeeShop.WebRazorPages.Services;
using CoffeeShop.WebRazorPages.ViewModels.Order;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoffeeShop.WebRazorPages.Pages;

public class MyOrdersModel : PageModel
{
    private readonly IOrderHttpService _orderHttpService;
    private readonly ILogger<MyOrdersModel> _logger;

    public MyOrdersModel(IOrderHttpService orderHttpService, ILogger<MyOrdersModel> logger)
    {
        _orderHttpService = orderHttpService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "ALL";

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? DetailId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? PayOrderId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool Created { get; set; }

    public List<OrderDtoViewModel> Orders { get; set; } = new();
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int ActiveCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }

    public OrderDetailViewModel? SelectedOrderDetail { get; set; }
    public VnPayPaymentResponseViewModel? ActiveVnPayQr { get; set; }
    public VnPayReturnResultViewModel? VnPayReturnResult { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    private string? GetAccessToken() =>
        User.FindFirst(AuthConstants.AccessTokenClaimType)?.Value
        ?? HttpContext.Session.GetString(AuthConstants.JwtSessionKey)
        ?? Request.Cookies["access_token"];

    public async Task<IActionResult> OnGetAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Auth/Login", new { returnUrl = "/MyOrders" });
        }

        var role = User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("role");
        if (string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToPage("/Staff/Orders", new { view = "orders" });
        }

        // 1. Xử lý kết quả trả về từ cổng thanh toán VNPay (nếu có vnp_ResponseCode trên URL)
        if (Request.Query.ContainsKey("vnp_ResponseCode"))
        {
            var rawQuery = Request.QueryString.Value ?? string.Empty;
            var vnpResult = await _orderHttpService.ProcessVnPayReturnAsync(rawQuery);
            if (vnpResult.IsSuccess && vnpResult.Data != null)
            {
                VnPayReturnResult = vnpResult.Data;
                if (VnPayReturnResult.IsSuccess)
                {
                    SuccessMessage = $"Thanh toán VNPay thành công cho đơn #{VnPayReturnResult.OrderCode} ({VnPayReturnResult.Amount:N0}đ).";
                }
                else
                {
                    ErrorMessage = $"Đơn hàng #{VnPayReturnResult.OrderCode} chưa hoàn tất thanh toán ({VnPayReturnResult.Message}). Bạn có thể bấm nút 'Thanh toán VNPay' bên dưới để thanh toán lại bất cứ lúc nào.";
                }
            }
            else
            {
                var responseCode = Request.Query["vnp_ResponseCode"].ToString();
                var txnRef = Request.Query["vnp_TxnRef"].ToString();
                var orderCode = txnRef.Contains('_') ? txnRef.Split('_')[0] : txnRef;
                if (responseCode == "00")
                {
                    SuccessMessage = $"Đã ghi nhận giao dịch thanh toán VNPay cho đơn #{orderCode}.";
                }
                else
                {
                    ErrorMessage = $"Giao dịch thanh toán VNPay cho đơn #{orderCode} chưa hoàn tất (Mã phản hồi: {responseCode}). Bạn có thể bấm nút 'Thanh toán VNPay' trên đơn hàng để thanh toán lại.";
                }
            }
        }

        // 2. Lấy toàn bộ danh sách đơn cá nhân để tính thống kê KPI chính xác
        var allFilter = new OrderFilterViewModel { PageNumber = 1, PageSize = 100 };
        var allResponse = await _orderHttpService.GetMyOrdersAsync(allFilter, token);
        var allOrders = allResponse.Data?.Items?.ToList() ?? new List<OrderDtoViewModel>();

        TotalCount = allOrders.Count;
        PendingCount = allOrders.Count(o => o.IsPending);
        ActiveCount = allOrders.Count(o => o.IsProcessing || o.IsShipping);
        CompletedCount = allOrders.Count(o => o.IsCompleted);
        CancelledCount = allOrders.Count(o => o.IsCancelled);

        // 3. Áp dụng bộ lọc trạng thái & tìm kiếm mã đơn hàng qua Order.API
        var normalizedStatus = string.IsNullOrWhiteSpace(Status) ? "ALL" : Status.Trim().ToUpperInvariant();
        Status = normalizedStatus;

        if (normalizedStatus == "ALL" && string.IsNullOrWhiteSpace(Search))
        {
            Orders = allOrders;
        }
        else
        {
            var filter = new OrderFilterViewModel
            {
                Status = normalizedStatus == "ALL" ? null : normalizedStatus,
                OrderCode = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
                PageNumber = 1,
                PageSize = 100
            };

            var filteredResponse = await _orderHttpService.GetMyOrdersAsync(filter, token);
            Orders = filteredResponse.Data?.Items?.ToList() ?? new List<OrderDtoViewModel>();

            // Hỗ trợ tìm kiếm mở rộng theo tên món / ghi chú ngay trên danh sách nếu người dùng nhập tên món
            if (!string.IsNullOrWhiteSpace(Search) && Orders.Count == 0)
            {
                var keyword = Search.Trim().ToLowerInvariant();
                Orders = allOrders
                    .Where(o => normalizedStatus == "ALL" || o.NormalizedStatus == normalizedStatus)
                    .Where(o => (o.OrderCode?.ToLowerInvariant().Contains(keyword) ?? false)
                             || (o.ItemsSummary?.ToLowerInvariant().Contains(keyword) ?? false)
                             || (o.Note?.ToLowerInvariant().Contains(keyword) ?? false))
                    .ToList();
            }
        }

        // 4. Nếu người dùng bấm xem chi tiết đơn hàng (DetailId)
        if (DetailId.HasValue)
        {
            var detailRes = await _orderHttpService.GetOrderByIdAsync(DetailId.Value, token);
            if (detailRes.IsSuccess && detailRes.Data != null)
            {
                SelectedOrderDetail = detailRes.Data;
            }
            else
            {
                ErrorMessage = detailRes.Message ?? "Không thể tải chi tiết đơn hàng.";
            }
        }

        // 5. Nếu người dùng bấm Thanh toán VNPay (hoặc vừa chuyển hướng từ trang Đặt hàng sang) -> chuyển hướng thẳng sang VNPay Sandbox
        if (PayOrderId.HasValue)
        {
            var returnUrl = $"{Request.Scheme}://{Request.Host}/MyOrders";
            var qrRes = await _orderHttpService.GenerateVnPayUrlAsync(PayOrderId.Value, returnUrl, token);
            if (qrRes.IsSuccess && !string.IsNullOrWhiteSpace(qrRes.Data?.PaymentUrl))
            {
                return Redirect(qrRes.Data.PaymentUrl);
            }

            ErrorMessage = qrRes.Message ?? "Không thể khởi tạo đường dẫn thanh toán VNPay cho đơn hàng này.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCancelOrderAsync(Guid orderId, string? cancelReason, string? returnStatus, string? returnSearch)
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Auth/Login", new { returnUrl = "/MyOrders" });
        }

        var reason = string.IsNullOrWhiteSpace(cancelReason)
            ? "Khách hàng yêu cầu hủy đơn"
            : cancelReason.Trim();

        var response = await _orderHttpService.CancelOrderAsync(orderId, reason, token);
        if (response.IsSuccess)
        {
            SuccessMessage = $"Đã hủy đơn hàng #{response.Data?.OrderCode ?? orderId.ToString()[..8]} thành công.";
        }
        else
        {
            ErrorMessage = response.Message ?? "Không thể hủy đơn hàng này.";
        }

        return RedirectToPage("/MyOrders", new
        {
            Status = string.IsNullOrWhiteSpace(returnStatus) ? "ALL" : returnStatus,
            Search = returnSearch
        });
    }

    public async Task<IActionResult> OnPostGenerateQrAsync(Guid orderId, string? returnStatus, string? returnSearch)
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Auth/Login", new { returnUrl = "/MyOrders" });
        }

        var returnUrl = $"{Request.Scheme}://{Request.Host}/MyOrders";
        var qrRes = await _orderHttpService.GenerateVnPayUrlAsync(orderId, returnUrl, token);
        if (qrRes.IsSuccess && !string.IsNullOrWhiteSpace(qrRes.Data?.PaymentUrl))
        {
            return Redirect(qrRes.Data.PaymentUrl);
        }

        ErrorMessage = qrRes.Message ?? "Không thể khởi tạo cổng thanh toán VNPay cho đơn hàng này.";
        return RedirectToPage("/MyOrders", new
        {
            Status = string.IsNullOrWhiteSpace(returnStatus) ? "ALL" : returnStatus,
            Search = returnSearch
        });
    }

    public async Task<IActionResult> OnPostSimulateVnPayAsync(Guid orderId, string? returnStatus, string? returnSearch)
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Auth/Login", new { returnUrl = "/MyOrders" });
        }

        var response = await _orderHttpService.SimulateVnPayPaymentAsync(orderId, token);
        if (response.IsSuccess && response.Data != null && response.Data.IsSuccess)
        {
            SuccessMessage = $"Thanh toán VNPay thành công cho đơn #{response.Data.OrderCode} ({response.Data.Amount:N0}đ)! Đơn hàng đã chuyển sang trạng thái Đang pha chế.";
        }
        else
        {
            ErrorMessage = response.Message ?? "Không thể xác nhận thanh toán VNPay cho đơn hàng này.";
        }

        return RedirectToPage("/MyOrders", new
        {
            Status = string.IsNullOrWhiteSpace(returnStatus) ? "ALL" : returnStatus,
            Search = returnSearch
        });
    }
}
