using System.Security.Claims;
using System.Text.Json;
using CoffeeShop.WebRazorPages.Constants;
using CoffeeShop.WebRazorPages.Services;
using CoffeeShop.WebRazorPages.ViewModels.Engagement;
using CoffeeShop.WebRazorPages.ViewModels.Order;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoffeeShop.WebRazorPages.Pages;

public class OrderModel : PageModel
{
    private readonly IOrderHttpService _orderHttpService;
    private readonly IEngagementHttpService _engagementHttpService;
    private readonly IAuthHttpService _authHttpService;
    private readonly ILogger<OrderModel> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OrderModel(
        IOrderHttpService orderHttpService,
        IEngagementHttpService engagementHttpService,
        IAuthHttpService authHttpService,
        ILogger<OrderModel> logger)
    {
        _orderHttpService = orderHttpService;
        _engagementHttpService = engagementHttpService;
        _authHttpService = authHttpService;
        _logger = logger;
    }

    [BindProperty]
    public CheckoutFormInputViewModel CheckoutInput { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Voucher { get; set; }

    public List<VoucherViewModel> AvailableVouchers { get; set; } = new();

    public LoyaltyMemberProfileViewModel? MemberProfile { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    private string? GetAccessToken() =>
        User.FindFirst(AuthConstants.AccessTokenClaimType)?.Value
        ?? HttpContext.Session.GetString(AuthConstants.JwtSessionKey)
        ?? Request.Cookies["access_token"];

    private Guid? GetCurrentUserId()
    {
        var uidStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(uidStr, out var uid) ? uid : null;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Auth/Login", new { returnUrl = "/Order" });
        }

        if (!string.IsNullOrWhiteSpace(Voucher))
        {
            CheckoutInput.VoucherCode = Voucher.Trim().ToUpperInvariant();
        }

        // Tự động điền thông tin người nhận từ hồ sơ đã đăng nhập
        CheckoutInput.FullName = User.FindFirstValue(ClaimTypes.Name)
                              ?? User.Identity?.Name
                              ?? string.Empty;

        try
        {
            var profileRes = await _authHttpService.GetProfileAsync(token);
            if (profileRes.IsSuccess && profileRes.Data != null)
            {
                if (!string.IsNullOrWhiteSpace(profileRes.Data.FullName))
                    CheckoutInput.FullName = profileRes.Data.FullName;
                if (!string.IsNullOrWhiteSpace(profileRes.Data.PhoneNumber))
                    CheckoutInput.Phone = profileRes.Data.PhoneNumber;
                if (!string.IsNullOrWhiteSpace(profileRes.Data.Address))
                    CheckoutInput.Address = profileRes.Data.Address;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể lấy thông tin hồ sơ người dùng khi mở trang Checkout");
        }

        await LoadAvailableVouchersAsync(token);
        return Page();
    }

    public async Task<IActionResult> OnPostPlaceOrderAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Auth/Login", new { returnUrl = "/Order" });
        }

        await LoadAvailableVouchersAsync(token);

        if (string.IsNullOrWhiteSpace(CheckoutInput.FullName) || CheckoutInput.FullName.Trim().Length < 2)
        {
            ErrorMessage = "Vui lòng nhập họ và tên người nhận hợp lệ (tối thiểu 2 ký tự).";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(CheckoutInput.Phone))
        {
            ErrorMessage = "Vui lòng nhập số điện thoại liên hệ.";
            return Page();
        }

        var isPickup = string.Equals(CheckoutInput.OrderType, "pickup", StringComparison.OrdinalIgnoreCase);
        if (!isPickup && (string.IsNullOrWhiteSpace(CheckoutInput.Address) || CheckoutInput.Address.Trim().Length < 5))
        {
            ErrorMessage = "Vui lòng nhập địa chỉ giao hàng chi tiết (tối thiểu 5 ký tự).";
            return Page();
        }

        // Giải mã giỏ hàng từ CartJson
        List<BrowserCartItemDto>? cartItems = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(CheckoutInput.CartJson))
            {
                cartItems = JsonSerializer.Deserialize<List<BrowserCartItemDto>>(CheckoutInput.CartJson, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi giải mã giỏ hàng CartJson");
        }

        if (cartItems == null || cartItems.Count == 0)
        {
            ErrorMessage = "Giỏ hàng của bạn đang trống. Vui lòng chọn ít nhất 1 món trước khi đặt hàng.";
            return Page();
        }

        var orderItems = new List<CreateOrderItemInputViewModel>();
        decimal subtotal = 0m;

        foreach (var item in cartItems)
        {
            var pid = Guid.TryParse(item.Id, out var parsedGuid) ? parsedGuid : Guid.Empty;
            if (pid == Guid.Empty) continue;

            var qty = item.Quantity > 0 ? item.Quantity : 1;
            var unitPrice = item.Price >= 0 ? item.Price : 0m;
            var rawOptionLabel = string.IsNullOrWhiteSpace(item.Size) ? "Size M" : item.Size.Trim();
            var shortSize = ExtractShortSize(rawOptionLabel);
            var optionsSummary = rawOptionLabel.StartsWith("Size", StringComparison.OrdinalIgnoreCase)
                ? rawOptionLabel
                : $"Size {rawOptionLabel}";

            subtotal += unitPrice * qty;

            orderItems.Add(new CreateOrderItemInputViewModel
            {
                ProductId = pid,
                ProductName = string.IsNullOrWhiteSpace(item.Name) ? "Sản phẩm Lowland" : item.Name.Trim(),
                UnitPrice = unitPrice,
                Quantity = qty,
                Size = shortSize,
                OptionsSummary = optionsSummary
            });
        }

        if (orderItems.Count == 0)
        {
            ErrorMessage = "Danh sách sản phẩm trong giỏ hàng không hợp lệ.";
            return Page();
        }

        // 1. Tính giảm giá theo Đặc quyền Hạng thẻ thành viên (Tier Loyalty Benefit: Đồng 0%, Bạc 1%, Vàng 2%, Kim Cương 3%)
        var currentUserId = GetCurrentUserId();
        decimal tierDiscountPercent = MemberProfile?.DiscountPercent ?? 0m;
        decimal tierDiscount = tierDiscountPercent > 0m
            ? Math.Round(subtotal * (tierDiscountPercent / 100m), 0, MidpointRounding.AwayFromZero)
            : 0m;

        // 2. Kiểm tra mã giảm giá qua EngagementHttpService (nếu khách có nhập)
        decimal voucherDiscount = 0m;
        Guid? validatedVoucherId = null;

        if (!string.IsNullOrWhiteSpace(CheckoutInput.VoucherCode))
        {
            var voucherRes = await _engagementHttpService.ValidateVoucherAsync(new ValidateVoucherRequestViewModel
            {
                VoucherCode = CheckoutInput.VoucherCode.Trim().ToUpperInvariant(),
                OrderSubtotal = subtotal,
                UserId = currentUserId
            }, token);

            if (voucherRes.IsSuccess && voucherRes.Data != null && voucherRes.Data.IsValid)
            {
                voucherDiscount = voucherRes.Data.DiscountAmount;
                validatedVoucherId = voucherRes.Data.VoucherId;
            }
            else
            {
                ErrorMessage = voucherRes.Data?.Message ?? voucherRes.Message ?? "Mã giảm giá không hợp lệ hoặc bạn đã sử dụng mã này.";
                return Page();
            }
        }

        // 3. Kiểm tra sử dụng Điểm thưởng thành viên (1 điểm = 1.000 VNĐ)
        int actualPointsToRedeem = 0;
        decimal pointsDiscount = 0m;
        if (CheckoutInput.PointsToRedeem > 0 && currentUserId.HasValue)
        {
            var availablePoints = MemberProfile?.CurrentPoints ?? 0;
            if (CheckoutInput.PointsToRedeem > availablePoints)
            {
                ErrorMessage = $"Số điểm muốn sử dụng ({CheckoutInput.PointsToRedeem} điểm) vượt quá số dư điểm hiện có của bạn ({availablePoints} điểm).";
                return Page();
            }

            var remainingAfterTierAndVoucher = Math.Max(0m, subtotal - tierDiscount - voucherDiscount);
            var maxPointsForOrder = (int)Math.Floor(remainingAfterTierAndVoucher / 1000m);
            actualPointsToRedeem = Math.Min(CheckoutInput.PointsToRedeem, maxPointsForOrder);
            pointsDiscount = actualPointsToRedeem * 1000m;
        }

        decimal discountAmount = Math.Min(subtotal, tierDiscount + voucherDiscount + pointsDiscount);

        var locationDesc = isPickup
            ? $"Nhận tại quán: {CheckoutInput.StoreName ?? "Lowland Nguyễn Huệ (Quận 1)"}"
            : $"Giao đến: {CheckoutInput.Address?.Trim()}";

        var noteParts = new List<string>
        {
            $"Người nhận: {CheckoutInput.FullName.Trim()} ({CheckoutInput.Phone.Trim()})",
            locationDesc
        };

        if (tierDiscount > 0)
        {
            noteParts.Add($"Ưu đãi {MemberProfile?.TierName ?? "Hạng thẻ"} (-{tierDiscountPercent:0.##}%: -{tierDiscount:N0}đ)");
        }

        if (actualPointsToRedeem > 0)
        {
            noteParts.Add($"Dùng {actualPointsToRedeem} điểm thưởng (-{pointsDiscount:N0}đ)");
        }

        if (!string.IsNullOrWhiteSpace(CheckoutInput.Note))
        {
            noteParts.Add($"Ghi chú: {CheckoutInput.Note.Trim()}");
        }

        var paymentMethod = string.Equals(CheckoutInput.PaymentMethod, "VNPAY", StringComparison.OrdinalIgnoreCase)
            ? "VNPAY"
            : "CASH";

        var returnUrl = $"{Request.Scheme}://{Request.Host}/MyOrders";

        var createOrderRequest = new CreateOrderInputViewModel
        {
            CustomerName = CheckoutInput.FullName.Trim(),
            Items = orderItems,
            DiscountAmount = discountAmount,
            VoucherCode = string.IsNullOrWhiteSpace(CheckoutInput.VoucherCode) ? null : CheckoutInput.VoucherCode.Trim().ToUpperInvariant(),
            PaymentMethod = paymentMethod,
            Note = string.Join(" | ", noteParts),
            ReturnUrl = returnUrl
        };

        var result = await _orderHttpService.CreateOrderAsync(createOrderRequest, token);
        if (!result.IsSuccess || result.Data == null)
        {
            ErrorMessage = result.Message ?? "Không thể khởi tạo đơn hàng. Vui lòng kiểm tra lại thông tin.";
            return Page();
        }

        var createdOrder = result.Data;

        // Ghi nhận sử dụng Voucher vào Engagement.API nếu đơn hàng có áp dụng mã giảm giá hợp lệ
        if (validatedVoucherId.HasValue && currentUserId.HasValue && voucherDiscount > 0)
        {
            try
            {
                await _engagementHttpService.ApplyVoucherUsageAsync(new ApplyVoucherUsageRequestViewModel
                {
                    VoucherId = validatedVoucherId.Value,
                    UserId = currentUserId.Value,
                    OrderId = createdOrder.Id,
                    DiscountAmount = voucherDiscount
                }, token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể ghi nhận lượt sử dụng voucher {VoucherId} cho đơn {OrderId}", validatedVoucherId, createdOrder.Id);
            }
        }

        // Quy đổi & trừ điểm thưởng trong Engagement.API nếu khách hàng có sử dụng điểm
        if (actualPointsToRedeem > 0 && currentUserId.HasValue)
        {
            try
            {
                await _engagementHttpService.RedeemPointsAsync(new RedeemPointsRequestViewModel
                {
                    UserId = currentUserId.Value,
                    OrderId = createdOrder.Id,
                    PointsToRedeem = actualPointsToRedeem,
                    Reason = $"Đổi {actualPointsToRedeem} điểm trừ tiền đơn hàng #{createdOrder.OrderCode}"
                }, token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể trừ điểm thưởng cho đơn {OrderId}", createdOrder.Id);
            }
        }

        if (paymentMethod == "VNPAY")
        {
            var paymentUrl = createdOrder.VnPayPayment?.PaymentUrl;
            if (string.IsNullOrWhiteSpace(paymentUrl))
            {
                var qrRes = await _orderHttpService.GenerateVnPayUrlAsync(createdOrder.Id, returnUrl, token);
                paymentUrl = qrRes.Data?.PaymentUrl;
            }

            if (!string.IsNullOrWhiteSpace(paymentUrl))
            {
                return Redirect(paymentUrl);
            }

            TempData["SuccessMessage"] = $"Đã tạo đơn hàng #{createdOrder.OrderCode} thành công! Bạn có thể bấm 'Thanh toán VNPay' trên đơn hàng để chuyển tới trang VNPay.";
            return RedirectToPage("/MyOrders", new { Created = true });
        }

        TempData["SuccessMessage"] = $"Đặt đơn hàng #{createdOrder.OrderCode} thành công! Tổng thanh toán: {createdOrder.FinalAmount:N0}đ.";
        return RedirectToPage("/MyOrders", new { Created = true, DetailId = createdOrder.Id });
    }

    public async Task<IActionResult> OnGetValidateVoucherAsync(string code, decimal orderAmount)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new JsonResult(new { isSuccess = false, message = "Vui lòng nhập mã giảm giá." });
        }

        var token = GetAccessToken();
        var currentUserId = GetCurrentUserId();

        var response = await _engagementHttpService.ValidateVoucherAsync(new ValidateVoucherRequestViewModel
        {
            VoucherCode = code.Trim().ToUpperInvariant(),
            OrderSubtotal = orderAmount,
            UserId = currentUserId
        }, token);

        if (response.IsSuccess && response.Data != null && response.Data.IsValid)
        {
            return new JsonResult(new
            {
                isSuccess = true,
                discountAmount = response.Data.DiscountAmount,
                message = response.Data.Message ?? $"Áp dụng mã {code.ToUpperInvariant()} thành công (-{response.Data.DiscountAmount:N0}đ)."
            });
        }

        return new JsonResult(new
        {
            isSuccess = false,
            discountAmount = 0,
            message = response.Data?.Message ?? response.Message ?? "Mã giảm giá không hợp lệ hoặc chưa đủ điều kiện."
        });
    }

    private static string ExtractShortSize(string rawSize)
    {
        var upper = rawSize.ToUpperInvariant();
        if (upper.Contains("SIZE L") || upper == "L" || upper.StartsWith("L ")) return "L";
        if (upper.Contains("SIZE S") || upper == "S" || upper.StartsWith("S ")) return "S";
        return "M";
    }

    private async Task LoadAvailableVouchersAsync(string? token = null)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                var profileRes = await _engagementHttpService.GetMemberProfileAsync(token);
                if (profileRes.IsSuccess && profileRes.Data != null)
                {
                    MemberProfile = profileRes.Data;
                }
            }

            var usedIds = new HashSet<Guid>();
            if (!string.IsNullOrWhiteSpace(token))
            {
                var usedRes = await _engagementHttpService.GetMyUsedVoucherIdsAsync(token);
                if (usedRes.IsSuccess && usedRes.Data != null)
                {
                    usedIds = usedRes.Data.ToHashSet();
                }
            }

            var res = await _engagementHttpService.GetActiveVouchersAsync();
            if (res.IsSuccess && res.Data != null)
            {
                AvailableVouchers = res.Data
                    .Where(v => v.IsValidNow && !usedIds.Contains(v.Id))
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể tải danh sách voucher và điểm thưởng khách hàng");
        }
    }

    private class BrowserCartItemDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? Size { get; set; }
        public string? Image { get; set; }
    }
}
