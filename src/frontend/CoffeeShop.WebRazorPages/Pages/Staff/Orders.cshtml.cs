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
    private readonly IUserHttpService _userHttpService;
    private readonly ILogger<OrdersModel> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OrdersModel(
        IOrderHttpService orderHttpService,
        IEngagementHttpService engagementHttpService,
        IUserHttpService userHttpService,
        ILogger<OrdersModel> logger)
    {
        _orderHttpService = orderHttpService;
        _engagementHttpService = engagementHttpService;
        _userHttpService = userHttpService;
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

        // 5. Chuyển hướng trực tiếp sang cổng thanh toán VNPay Sandbox nếu có PayOrderId
        if (PayOrderId.HasValue)
        {
            var returnUrl = $"{Request.Scheme}://{Request.Host}/Staff/Orders?view=orders";
            var qrRes = await _orderHttpService.GenerateVnPayUrlAsync(PayOrderId.Value, returnUrl, token);
            if (qrRes.IsSuccess && !string.IsNullOrWhiteSpace(qrRes.Data?.PaymentUrl))
            {
                return Redirect(qrRes.Data.PaymentUrl);
            }

            ErrorMessage = qrRes.Message ?? "Không thể khởi tạo đường dẫn thanh toán VNPay cho đơn hàng này.";
        }

        return Page();
    }

    /// <summary>
    /// [22. Create Order - Staff POS]: Nhân viên tạo đơn tại quầy (hỗ trợ gắn SĐT khách hàng thành viên, áp dụng Voucher đủ ĐK & trừ Điểm thưởng)
    /// </summary>
    public async Task<IActionResult> OnPostCreatePosOrderAsync(
        Guid? customerId,
        string? customerPhone,
        string? customerName,
        string? note,
        string? voucherCode,
        int pointsToRedeem,
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

        // Nếu nhân viên có nhập SĐT nhưng chưa bấm tra cứu hoặc chưa truyền customerId, tự động tra cứu SĐT trong Auth.API
        Guid? targetCustomerId = (customerId.HasValue && customerId.Value != Guid.Empty) ? customerId.Value : null;
        if (!targetCustomerId.HasValue && !string.IsNullOrWhiteSpace(customerPhone))
        {
            var searchRes = await _userHttpService.SearchCustomersAsync(customerPhone.Trim(), token);
            var matchedUser = searchRes.Data?.FirstOrDefault(u =>
                string.Equals(u.PhoneNumber?.Trim(), customerPhone.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? searchRes.Data?.FirstOrDefault();

            if (matchedUser != null && matchedUser.Id != Guid.Empty)
            {
                targetCustomerId = matchedUser.Id;
                if (string.IsNullOrWhiteSpace(customerName))
                {
                    customerName = matchedUser.FullName;
                }
            }
        }

        // 1. Kiểm tra thông tin hạng thẻ thành viên (Tier Loyalty Benefit: Đồng 0%, Bạc 1%, Vàng 2%, Kim Cương 3%)
        LoyaltyMemberProfileViewModel? memberProfile = null;
        decimal tierDiscountPercent = 0m;
        decimal tierDiscount = 0m;
        if (targetCustomerId.HasValue)
        {
            var profileRes = await _engagementHttpService.GetMemberProfileByUserIdAsync(targetCustomerId.Value, token);
            memberProfile = profileRes.Data;
            tierDiscountPercent = memberProfile?.DiscountPercent ?? 0m;
            if (tierDiscountPercent > 0m)
            {
                tierDiscount = Math.Round(subtotal * (tierDiscountPercent / 100m), 0, MidpointRounding.AwayFromZero);
            }
        }

        // 2. Kiểm tra mã giảm giá (Voucher) cho khách hàng tại POS
        decimal voucherDiscount = 0m;
        Guid? validatedVoucherId = null;
        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            var vRes = await _engagementHttpService.ValidateVoucherAsync(new ValidateVoucherRequestViewModel
            {
                VoucherCode = voucherCode.Trim().ToUpperInvariant(),
                OrderSubtotal = subtotal,
                UserId = targetCustomerId
            }, token);

            if (vRes.IsSuccess && vRes.Data != null && vRes.Data.IsValid)
            {
                voucherDiscount = vRes.Data.DiscountAmount;
                validatedVoucherId = vRes.Data.VoucherId;
            }
            else
            {
                ErrorMessage = vRes.Data?.Message ?? vRes.Message ?? "Mã voucher không hợp lệ hoặc khách hàng đã sử dụng mã này.";
                return RedirectToPage("/Staff/Orders", new { View = "pos" });
            }
        }

        // 3. Kiểm tra & quy đổi điểm thưởng khách hàng tại POS (1 điểm = 1.000đ)
        int actualPointsToRedeem = 0;
        decimal pointsDiscount = 0m;
        if (pointsToRedeem > 0)
        {
            if (!targetCustomerId.HasValue)
            {
                ErrorMessage = "Vui lòng kiểm tra SĐT tài khoản khách hàng thành viên trước khi sử dụng điểm thưởng.";
                return RedirectToPage("/Staff/Orders", new { View = "pos" });
            }

            var availPoints = memberProfile?.CurrentPoints ?? 0;
            if (pointsToRedeem > availPoints)
            {
                ErrorMessage = $"Khách hàng hiện có {availPoints} điểm, không đủ để trừ {pointsToRedeem} điểm.";
                return RedirectToPage("/Staff/Orders", new { View = "pos" });
            }

            var remainingAfterTierAndVoucher = Math.Max(0m, subtotal - tierDiscount - voucherDiscount);
            var maxPointsForOrder = (int)Math.Floor(remainingAfterTierAndVoucher / 1000m);
            actualPointsToRedeem = Math.Min(pointsToRedeem, maxPointsForOrder);
            pointsDiscount = actualPointsToRedeem * 1000m;
        }

        decimal totalDiscount = Math.Min(subtotal, tierDiscount + voucherDiscount + pointsDiscount);

        var normalizedMethod = string.Equals(paymentMethod, "VNPAY", StringComparison.OrdinalIgnoreCase) ? "VNPAY" : "CASH";
        var finalCustomerName = string.IsNullOrWhiteSpace(customerName)
            ? $"Khách tại quầy #{DateTime.Now:HHmm}"
            : customerName.Trim();

        var noteParts = new List<string> { "[POS]" };
        if (!string.IsNullOrWhiteSpace(customerPhone))
        {
            noteParts.Add($"SĐT: {customerPhone.Trim()}");
        }
        if (tierDiscount > 0)
        {
            noteParts.Add($"Ưu đãi {memberProfile?.TierName ?? "Hạng thẻ"} (-{tierDiscountPercent:0.##}%: -{tierDiscount:N0}đ)");
        }
        if (actualPointsToRedeem > 0)
        {
            noteParts.Add($"Trừ {actualPointsToRedeem} điểm (-{pointsDiscount:N0}đ)");
        }
        if (!string.IsNullOrWhiteSpace(note))
        {
            noteParts.Add(note.Trim());
        }

        var createReq = new CreateOrderInputViewModel
        {
            CustomerId = targetCustomerId,
            CustomerName = finalCustomerName,
            Items = items,
            DiscountAmount = totalDiscount >= 0 ? totalDiscount : 0m,
            VoucherCode = string.IsNullOrWhiteSpace(voucherCode) ? null : voucherCode.Trim().ToUpperInvariant(),
            PaymentMethod = normalizedMethod,
            Note = string.Join(" | ", noteParts),
            ReturnUrl = $"{Request.Scheme}://{Request.Host}/Staff/Orders?view=orders"
        };

        var createRes = await _orderHttpService.CreateOrderAsync(createReq, token);
        if (!createRes.IsSuccess || createRes.Data == null)
        {
            ErrorMessage = createRes.Message ?? "Không thể tạo đơn hàng tại quầy.";
            return RedirectToPage("/Staff/Orders", new { View = "pos" });
        }

        var createdOrder = createRes.Data;

        // Ghi nhận sử dụng Voucher vào Engagement.API nếu đơn POS có áp dụng mã hợp lệ
        if (validatedVoucherId.HasValue && targetCustomerId.HasValue && voucherDiscount > 0)
        {
            try
            {
                await _engagementHttpService.ApplyVoucherUsageAsync(new ApplyVoucherUsageRequestViewModel
                {
                    VoucherId = validatedVoucherId.Value,
                    UserId = targetCustomerId.Value,
                    OrderId = createdOrder.Id,
                    DiscountAmount = voucherDiscount
                }, token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể ghi nhận lượt sử dụng voucher {VoucherId} cho đơn POS {OrderId}", validatedVoucherId, createdOrder.Id);
            }
        }

        // Trừ điểm thưởng trong tài khoản khách hàng nếu nhân viên nhập điểm trừ tiền tại POS
        if (actualPointsToRedeem > 0 && targetCustomerId.HasValue)
        {
            try
            {
                await _engagementHttpService.RedeemPointsAsync(new RedeemPointsRequestViewModel
                {
                    UserId = targetCustomerId.Value,
                    OrderId = createdOrder.Id,
                    PointsToRedeem = actualPointsToRedeem,
                    Reason = $"Trừ {actualPointsToRedeem} điểm tại quầy POS cho đơn #{createdOrder.OrderCode}"
                }, token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể trừ điểm thưởng cho khách hàng {CustomerId} ở đơn POS {OrderId}", targetCustomerId, createdOrder.Id);
            }
        }

        // Nếu nhân viên chọn VNPay -> chuyển hướng trực tiếp sang cổng thanh toán VNPay Sandbox
        if (normalizedMethod == "VNPAY")
        {
            var paymentUrl = createdOrder.VnPayPayment?.PaymentUrl;
            if (string.IsNullOrWhiteSpace(paymentUrl))
            {
                var returnUrl = $"{Request.Scheme}://{Request.Host}/Staff/Orders?view=orders";
                var qrRes = await _orderHttpService.GenerateVnPayUrlAsync(createdOrder.Id, returnUrl, token);
                paymentUrl = qrRes.Data?.PaymentUrl;
            }

            if (!string.IsNullOrWhiteSpace(paymentUrl))
            {
                return Redirect(paymentUrl);
            }

            SuccessMessage = $"Đã tạo đơn #{createdOrder.OrderCode} ({createdOrder.FinalAmount:N0}đ). Bấm 'Thanh toán VNPay' trên đơn hàng để chuyển sang cổng VNPay.";
            return RedirectToPage("/Staff/Orders", new { View = "orders" });
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
            if (string.Equals(nextStatus, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            {
                SuccessMessage = $"Đã hoàn thành đơn #{res.Data.OrderCode} và tự động cộng điểm thưởng vào tài khoản khách hàng!";
            }
            else
            {
                SuccessMessage = $"Đã cập nhật trạng thái đơn #{res.Data.OrderCode} sang '{res.Data.StatusDisplayName}'.";
            }
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
    /// [28. Generate VNPay URL & Redirect]: Nhân viên bấm thanh toán VNPay cho đơn chưa thanh toán -> dẫn thẳng tới VNPay Sandbox
    /// </summary>
    public async Task<IActionResult> OnPostGenerateQrAsync(Guid orderId, string? returnStatus, string? returnSearch)
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Auth/Login", new { returnUrl = "/Staff/Orders" });
        }

        var returnUrl = $"{Request.Scheme}://{Request.Host}/Staff/Orders?view=orders";
        var qrRes = await _orderHttpService.GenerateVnPayUrlAsync(orderId, returnUrl, token);
        if (qrRes.IsSuccess && !string.IsNullOrWhiteSpace(qrRes.Data?.PaymentUrl))
        {
            return Redirect(qrRes.Data.PaymentUrl);
        }

        ErrorMessage = qrRes.Message ?? "Không thể mở cổng thanh toán VNPay cho đơn hàng này.";
        return RedirectToPage("/Staff/Orders", new
        {
            View = "orders",
            Status = string.IsNullOrWhiteSpace(returnStatus) ? "ALL" : returnStatus,
            Search = returnSearch
        });
    }

    public async Task<IActionResult> OnPostSimulateVnPayAsync(Guid orderId, string? returnStatus, string? returnSearch)
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Auth/Login", new { returnUrl = "/Staff/Orders" });
        }

        var res = await _orderHttpService.SimulateVnPayPaymentAsync(orderId, token);
        if (res.IsSuccess && res.Data != null && res.Data.IsSuccess)
        {
            SuccessMessage = $"Đã xác nhận thanh toán VNPay thành công cho đơn #{res.Data.OrderCode} ({res.Data.Amount:N0}đ)!";
        }
        else
        {
            ErrorMessage = res.Message ?? "Không thể xác nhận thanh toán VNPay.";
        }

        return RedirectToPage("/Staff/Orders", new
        {
            View = "orders",
            Status = string.IsNullOrWhiteSpace(returnStatus) ? "ALL" : returnStatus,
            Search = returnSearch
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

    /// <summary>
    /// Tra cứu SĐT khách hàng tại quầy POS: Kiểm tra tài khoản tồn tại, lấy số điểm thưởng khả dụng và danh sách Voucher thỏa mãn điều kiện của khách đó
    /// </summary>
    public async Task<IActionResult> OnGetLookupCustomerAsync(string phone, decimal orderAmount = 0m)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return new JsonResult(new { isSuccess = false, message = "Vui lòng nhập số điện thoại khách hàng để kiểm tra." });
        }

        var token = GetAccessToken();
        var cleanPhone = phone.Trim();

        var searchRes = await _userHttpService.SearchCustomersAsync(cleanPhone, token);
        var users = searchRes.Data?.ToList() ?? new List<CoffeeShop.WebRazorPages.ViewModels.Auth.UserViewModel>();

        var matchedUser = users.FirstOrDefault(u =>
            string.Equals(u.PhoneNumber?.Trim(), cleanPhone, StringComparison.OrdinalIgnoreCase))
            ?? users.FirstOrDefault();

        if (matchedUser == null || matchedUser.Id == Guid.Empty)
        {
            return new JsonResult(new
            {
                isSuccess = false,
                message = $"Không tìm thấy tài khoản thành viên nào với SĐT '{cleanPhone}'."
            });
        }

        // Lấy hồ sơ tích điểm của khách hàng
        var profileRes = await _engagementHttpService.GetMemberProfileByUserIdAsync(matchedUser.Id, token);
        var profile = profileRes.Data;

        // Lấy danh sách voucher khách hàng này đã sử dụng để lọc ra những voucher còn dùng được
        var usedRes = await _engagementHttpService.GetUserUsedVoucherIdsAsync(matchedUser.Id, token);
        var usedIds = usedRes.Data?.ToHashSet() ?? new HashSet<Guid>();

        var activeVouchersRes = await _engagementHttpService.GetActiveVouchersAsync();
        var availableVouchers = (activeVouchersRes.Data ?? Enumerable.Empty<VoucherViewModel>())
            .Where(v => v.IsValidNow && !usedIds.Contains(v.Id))
            .OrderBy(v => v.MinOrderValue)
            .Select(v => new
            {
                id = v.Id,
                code = v.Code,
                title = v.Title,
                discountType = v.DiscountType,
                discountValue = v.DiscountValue,
                maxDiscountAmount = v.MaxDiscountAmount,
                minOrderValue = v.MinOrderValue,
                isEligible = orderAmount >= v.MinOrderValue
            })
            .ToList();

        return new JsonResult(new
        {
            isSuccess = true,
            message = $"Tìm thấy thành viên: {matchedUser.FullName} ({profile?.TierName ?? "Hạng Đồng"} • Giảm {profile?.DiscountPercent ?? 0m:0.##}% • Tích điểm x{profile?.PointMultiplier ?? 1.0m:0.##} • {profile?.CurrentPoints ?? 0} điểm)",
            customer = new
            {
                id = matchedUser.Id,
                fullName = matchedUser.FullName,
                phoneNumber = matchedUser.PhoneNumber ?? cleanPhone,
                email = matchedUser.Email,
                tierName = profile?.TierName ?? "Hạng Đồng",
                currentPoints = profile?.CurrentPoints ?? 0,
                totalAccumulatedPoints = profile?.TotalAccumulatedPoints ?? 0,
                pointMultiplier = profile?.PointMultiplier ?? 1.0m,
                discountPercent = profile?.DiscountPercent ?? 0m,
                benefitsDescription = profile?.BenefitsDescription
            },
            vouchers = availableVouchers
        });
    }

    public async Task<IActionResult> OnGetValidateVoucherAsync(string code, decimal orderAmount, Guid? customerId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new JsonResult(new { isSuccess = false, message = "Vui lòng nhập mã giảm giá." });
        }

        var token = GetAccessToken();
        var res = await _engagementHttpService.ValidateVoucherAsync(new ValidateVoucherRequestViewModel
        {
            VoucherCode = code.Trim().ToUpperInvariant(),
            OrderSubtotal = orderAmount,
            UserId = (customerId.HasValue && customerId.Value != Guid.Empty) ? customerId.Value : null
        }, token);

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
