using System.Security.Claims;
using System.Text.Json;
using CoffeeShop.WebRazorPages.Constants;
using CoffeeShop.WebRazorPages.Services;
using CoffeeShop.WebRazorPages.ViewModels.Engagement;
using CoffeeShop.WebRazorPages.ViewModels.Order;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoffeeShop.WebRazorPages.Pages.Staff;

public class OrdersModel : PageModel
{
    private readonly IOrderHttpService _orderHttpService;
    private readonly IEngagementHttpService _engagementHttpService;
    private readonly ILogger<OrdersModel> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OrdersModel(
        IOrderHttpService orderHttpService,
        IEngagementHttpService engagementHttpService,
        ILogger<OrdersModel> logger)
    {
        _orderHttpService = orderHttpService;
        _engagementHttpService = engagementHttpService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string View { get; set; } = "pos";

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "ALL";

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? DetailId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? PayOrderId { get; set; }

    public List<PosCategoryItemViewModel> Categories { get; set; } = new();
    public List<PosProductItemViewModel> Products { get; set; } = new();
    public List<OrderDtoViewModel> Orders { get; set; } = new();

    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int ProcessingCount { get; set; }
    public int ShippingCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int UnpaidCount { get; set; }
    public decimal TotalPaidRevenue { get; set; }

    public OrderDetailViewModel? SelectedOrderDetail { get; set; }
    public VnPayPaymentResponseViewModel? ActiveVnPayQr { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    private string? GetAccessToken() =>
        User.FindFirst(AuthConstants.AccessTokenClaimType)?.Value
        ?? HttpContext.Session.GetString(AuthConstants.JwtSessionKey)
        ?? Request.Cookies["access_token"];

    private string? GetCurrentRole(string? token)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        if (!string.IsNullOrWhiteSpace(role)) return role;

        if (!string.IsNullOrWhiteSpace(token))
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length == 3)
                {
                    var payload = parts[1].Replace('-', '+').Replace('_', '/');
                    switch (payload.Length % 4)
                    {
                        case 2: payload += "=="; break;
                        case 3: payload += "="; break;
                    }
                    var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("role", out var rProp))
                        return rProp.GetString();
                    if (doc.RootElement.TryGetProperty(ClaimTypes.Role, out var crProp))
                        return crProp.GetString();
                }
            }
            catch { }
        }
        return null;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Auth/Login", new { returnUrl = "/Staff/Orders" });
        }

        var role = GetCurrentRole(token);
        if (!string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToPage("/AccessDenied");
        }

        // Xử lý khi cổng VNPay redirect về /Staff/Orders
        if (Request.Query.ContainsKey("vnp_ResponseCode"))
        {
            View = "orders";
            var rawQuery = Request.QueryString.Value ?? string.Empty;
            var vnpResult = await _orderHttpService.ProcessVnPayReturnAsync(rawQuery);
            if (vnpResult.IsSuccess && vnpResult.Data != null)
            {
                if (vnpResult.Data.IsSuccess)
                {
                    SuccessMessage = $"Thanh toán VNPay thành công cho đơn #{vnpResult.Data.OrderCode} ({vnpResult.Data.Amount:N0}đ).";
                }
                else
                {
                    ErrorMessage = $"Đơn #{vnpResult.Data.OrderCode} chưa hoàn tất thanh toán VNPay ({vnpResult.Data.Message}). Nhân viên có thể bấm 'Thu tiền mặt' hoặc 'Tạo QR VNPay' lại.";
                }
            }
        }

        View = string.Equals(View, "orders", StringComparison.OrdinalIgnoreCase) ? "orders" : "pos";

        // 1. Tải danh mục & sản phẩm cho màn hình POS
        Categories = await _orderHttpService.GetPosCategoriesAsync();
        Products = await _orderHttpService.GetPosProductsAsync();

        // 2. Tải danh sách toàn bộ đơn hàng cho Staff
        var allFilter = new OrderFilterViewModel { PageNumber = 1, PageSize = 100 };
        var allRes = await _orderHttpService.GetAllOrdersAsync(allFilter, token);
        var allOrders = allRes.Data?.Items?.ToList() ?? new List<OrderDtoViewModel>();

        TotalCount = allOrders.Count;
        PendingCount = allOrders.Count(o => o.IsPending);
        ProcessingCount = allOrders.Count(o => o.IsProcessing);
        ShippingCount = allOrders.Count(o => o.IsShipping);
        CompletedCount = allOrders.Count(o => o.IsCompleted);
        CancelledCount = allOrders.Count(o => o.IsCancelled);
        UnpaidCount = allOrders.Count(o => !o.IsPaid && !o.IsCancelled);
        TotalPaidRevenue = allOrders.Where(o => o.IsPaid && !o.IsCancelled).Sum(o => o.FinalAmount);

        // 3. Áp dụng bộ lọc & tìm kiếm đơn hàng qua Order.API
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

            var filteredRes = await _orderHttpService.GetAllOrdersAsync(filter, token);
            Orders = filteredRes.Data?.Items?.ToList() ?? new List<OrderDtoViewModel>();

            // Tìm kiếm mở rộng theo tên khách hàng hoặc tên món nếu không khớp mã đơn
            if (!string.IsNullOrWhiteSpace(Search) && Orders.Count == 0)
            {
                var kw = Search.Trim().ToLowerInvariant();
                Orders = allOrders
                    .Where(o => normalizedStatus == "ALL" || o.NormalizedStatus == normalizedStatus)
                    .Where(o => (o.OrderCode?.ToLowerInvariant().Contains(kw) ?? false)
                             || (o.CustomerName?.ToLowerInvariant().Contains(kw) ?? false)
                             || (o.ItemsSummary?.ToLowerInvariant().Contains(kw) ?? false)
                             || (o.Note?.ToLowerInvariant().Contains(kw) ?? false))
                    .ToList();
            }
        }

        // 4. Mở Modal Chi tiết đơn hàng nếu có DetailId
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

        // 5. Mở Modal QR VNPay nếu có PayOrderId
        if (PayOrderId.HasValue)
        {
            var returnUrl = $"{Request.Scheme}://{Request.Host}/Staff/Orders?view=orders";
            var qrRes = await _orderHttpService.GenerateVnPayUrlAsync(PayOrderId.Value, returnUrl, token);
            if (qrRes.IsSuccess && qrRes.Data != null)
            {
                ActiveVnPayQr = qrRes.Data;
            }
            else
            {
                ErrorMessage = qrRes.Message ?? "Không thể khởi tạo mã QR VNPay cho đơn hàng này.";
            }
        }

        return Page();
    }

    /// <summary>
    /// [22. Create Order - Staff POS]: Nhân viên tạo đơn tại quầy
    /// </summary>
    public async Task<IActionResult> OnPostCreatePosOrderAsync(
        string? customerName,
        string? note,
        string? voucherCode,
        decimal discountAmount,
        string paymentMethod,
        bool collectCashNow,
        string posCartJson)
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Auth/Login", new { returnUrl = "/Staff/Orders" });
        }

        List<PosCartItemDto>? cartItems = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(posCartJson))
            {
                cartItems = JsonSerializer.Deserialize<List<PosCartItemDto>>(posCartJson, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi giải mã giỏ hàng POS");
        }

        if (cartItems == null || cartItems.Count == 0)
        {
            ErrorMessage = "Vui lòng chọn ít nhất 1 món vào hóa đơn POS trước khi tạo đơn.";
            return RedirectToPage("/Staff/Orders", new { View = "pos" });
        }

        var items = new List<CreateOrderItemInputViewModel>();
        decimal subtotal = 0m;
        foreach (var c in cartItems)
        {
            if (c.ProductId == Guid.Empty) continue;
            var qty = c.Quantity > 0 ? c.Quantity : 1;
            subtotal += c.UnitPrice * qty;
            items.Add(new CreateOrderItemInputViewModel
            {
                ProductId = c.ProductId,
                ProductName = string.IsNullOrWhiteSpace(c.ProductName) ? "Món tại quầy" : c.ProductName.Trim(),
                UnitPrice = c.UnitPrice,
                Quantity = qty,
                Size = string.IsNullOrWhiteSpace(c.Size) ? "M" : c.Size.Trim(),
                OptionsSummary = string.IsNullOrWhiteSpace(c.OptionsSummary) ? $"Size {c.Size ?? "M"}" : c.OptionsSummary
            });
        }

        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            var vRes = await _engagementHttpService.ValidateVoucherAsync(new ValidateVoucherRequestViewModel
            {
                VoucherCode = voucherCode.Trim().ToUpperInvariant(),
                OrderSubtotal = subtotal
            });
            if (vRes.IsSuccess && vRes.Data != null && vRes.Data.IsValid)
            {
                discountAmount = vRes.Data.DiscountAmount;
            }
        }

        var normalizedMethod = string.Equals(paymentMethod, "VNPAY", StringComparison.OrdinalIgnoreCase) ? "VNPAY" : "CASH";
        var finalCustomerName = string.IsNullOrWhiteSpace(customerName)
            ? $"Khách tại quầy #{DateTime.Now:HHmm}"
            : customerName.Trim();

        var createReq = new CreateOrderInputViewModel
        {
            CustomerName = finalCustomerName,
            Items = items,
            DiscountAmount = discountAmount >= 0 ? discountAmount : 0m,
            VoucherCode = string.IsNullOrWhiteSpace(voucherCode) ? null : voucherCode.Trim().ToUpperInvariant(),
            PaymentMethod = normalizedMethod,
            Note = string.IsNullOrWhiteSpace(note) ? "Đơn tại quầy POS" : $"[POS] {note.Trim()}",
            ReturnUrl = $"{Request.Scheme}://{Request.Host}/Staff/Orders?view=orders"
        };

        var createRes = await _orderHttpService.CreateOrderAsync(createReq, token);
        if (!createRes.IsSuccess || createRes.Data == null)
        {
            ErrorMessage = createRes.Message ?? "Không thể tạo đơn hàng tại quầy.";
            return RedirectToPage("/Staff/Orders", new { View = "pos" });
        }

        var createdOrder = createRes.Data;

        // Nếu nhân viên chọn VNPay -> chuyển sang tab Điều phối và mở ngay Modal QR VNPay
        if (normalizedMethod == "VNPAY")
        {
            SuccessMessage = $"Đã tạo đơn #{createdOrder.OrderCode} ({createdOrder.FinalAmount:N0}đ). Vui lòng cho khách quét mã QR VNPay để thanh toán.";
            return RedirectToPage("/Staff/Orders", new { View = "orders", PayOrderId = createdOrder.Id });
        }

        // Nếu nhân viên chọn Tiền mặt và tích chọn "Đã thu tiền mặt ngay tại quầy"
        if (collectCashNow)
        {
            var cashRes = await _orderHttpService.ConfirmCashPaymentAsync(createdOrder.Id, null, "Thu tiền mặt trực tiếp tại quầy POS", token);
            if (cashRes.IsSuccess)
            {
                SuccessMessage = $"Đã tạo đơn #{createdOrder.OrderCode} và xác nhận thu tiền mặt ({createdOrder.FinalAmount:N0}đ) thành công! Đơn đã chuyển sang Đang pha chế.";
            }
            else
            {
                SuccessMessage = $"Đã tạo đơn #{createdOrder.OrderCode}. Lưu ý: Chưa xác nhận thu tiền ({cashRes.Message}).";
            }
        }
        else
        {
            SuccessMessage = $"Đã tạo đơn #{createdOrder.OrderCode} (Chưa thu tiền). Vui lòng bấm 'Thu tiền mặt' hoặc 'Tạo QR VNPay' trước khi hoàn thành đơn.";
        }

        return RedirectToPage("/Staff/Orders", new { View = "orders" });
    }

    /// <summary>
    /// [26. Update Order Status]: PENDING -> PROCESSING -> SHIPPING -> COMPLETED (Bắt buộc thu tiền trước khi COMPLETED)
    /// </summary>
    public async Task<IActionResult> OnPostUpdateStatusAsync(Guid orderId, string nextStatus, string? note, string? returnStatus, string? returnSearch)
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Auth/Login", new { returnUrl = "/Staff/Orders" });
        }

        var res = await _orderHttpService.UpdateOrderStatusAsync(orderId, nextStatus, note, token);
        if (res.IsSuccess && res.Data != null)
        {
            SuccessMessage = $"Đã cập nhật trạng thái đơn #{res.Data.OrderCode} sang '{res.Data.StatusDisplayName}'.";
        }
        else
        {
            ErrorMessage = res.Message ?? "Không thể cập nhật trạng thái đơn hàng.";
        }

        return RedirectToPage("/Staff/Orders", new
        {
            View = "orders",
            Status = string.IsNullOrWhiteSpace(returnStatus) ? "ALL" : returnStatus,
            Search = returnSearch
        });
    }

    /// <summary>
    /// [29. Confirm Payment - Cash]: Nhân viên xác nhận đã thu tiền mặt
    /// </summary>
    public async Task<IActionResult> OnPostConfirmCashAsync(Guid orderId, string? returnStatus, string? returnSearch)
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Auth/Login", new { returnUrl = "/Staff/Orders" });
        }

        var res = await _orderHttpService.ConfirmCashPaymentAsync(orderId, null, "Nhân viên xác nhận đã thu tiền mặt", token);
        if (res.IsSuccess)
        {
            SuccessMessage = "Đã xác nhận thu tiền mặt thành công! Bây giờ bạn có thể tiếp tục xử lý và bấm Hoàn thành đơn.";
        }
        else
        {
            ErrorMessage = res.Message ?? "Không thể xác nhận thu tiền mặt.";
        }

        return RedirectToPage("/Staff/Orders", new
        {
            View = "orders",
            Status = string.IsNullOrWhiteSpace(returnStatus) ? "ALL" : returnStatus,
            Search = returnSearch
        });
    }

    /// <summary>
    /// [28. Generate VNPay QR]: Nhân viên mở / tạo lại mã QR VNPay cho đơn chưa thanh toán
    /// </summary>
    public IActionResult OnPostGenerateQr(Guid orderId, string? returnStatus, string? returnSearch)
    {
        return RedirectToPage("/Staff/Orders", new
        {
            View = "orders",
            Status = string.IsNullOrWhiteSpace(returnStatus) ? "ALL" : returnStatus,
            Search = returnSearch,
            PayOrderId = orderId
        });
    }

    /// <summary>
    /// [27. Cancel Order - Staff]: Nhân viên hủy đơn kèm lý do bắt buộc
    /// </summary>
    public async Task<IActionResult> OnPostCancelOrderAsync(Guid orderId, string? cancelReason, string? returnStatus, string? returnSearch)
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Auth/Login", new { returnUrl = "/Staff/Orders" });
        }

        if (string.IsNullOrWhiteSpace(cancelReason))
        {
            ErrorMessage = "Nhân viên bắt buộc phải nhập lý do khi hủy đơn hàng.";
            return RedirectToPage("/Staff/Orders", new
            {
                View = "orders",
                Status = string.IsNullOrWhiteSpace(returnStatus) ? "ALL" : returnStatus,
                Search = returnSearch
            });
        }

        var res = await _orderHttpService.CancelOrderAsync(orderId, cancelReason.Trim(), token);
        if (res.IsSuccess)
        {
            SuccessMessage = $"Đã hủy đơn hàng #{res.Data?.OrderCode ?? orderId.ToString()[..8]} thành công.";
        }
        else
        {
            ErrorMessage = res.Message ?? "Không thể hủy đơn hàng.";
        }

        return RedirectToPage("/Staff/Orders", new
        {
            View = "orders",
            Status = string.IsNullOrWhiteSpace(returnStatus) ? "ALL" : returnStatus,
            Search = returnSearch
        });
    }

    public async Task<IActionResult> OnGetValidateVoucherAsync(string code, decimal orderAmount)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new JsonResult(new { isSuccess = false, message = "Vui lòng nhập mã giảm giá." });
        }

        var res = await _engagementHttpService.ValidateVoucherAsync(new ValidateVoucherRequestViewModel
        {
            VoucherCode = code.Trim().ToUpperInvariant(),
            OrderSubtotal = orderAmount
        });

        if (res.IsSuccess && res.Data != null && res.Data.IsValid)
        {
            return new JsonResult(new
            {
                isSuccess = true,
                discountAmount = res.Data.DiscountAmount,
                message = res.Data.Message ?? $"Giảm {res.Data.DiscountAmount:N0}đ"
            });
        }

        return new JsonResult(new
        {
            isSuccess = false,
            discountAmount = 0,
            message = res.Data?.Message ?? res.Message ?? "Mã voucher không hợp lệ."
        });
    }

    private class PosCartItemDto
    {
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? Size { get; set; }
        public string? OptionsSummary { get; set; }
    }
}
