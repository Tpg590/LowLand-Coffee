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

    public List<VoucherViewModel> AvailableVouchers { get; set; } = new();

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
            return RedirectToPage("/Auth/Login", new { returnUrl = "/Order" });
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

        await LoadAvailableVouchersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostPlaceOrderAsync()
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToPage("/Auth/Login", new { returnUrl = "/Order" });
        }

        await LoadAvailableVouchersAsync();

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
            var sizeLabel = string.IsNullOrWhiteSpace(item.Size) ? "M" : item.Size.Trim();

            subtotal += unitPrice * qty;

            orderItems.Add(new CreateOrderItemInputViewModel
            {
                ProductId = pid,
                ProductName = string.IsNullOrWhiteSpace(item.Name) ? "Sản phẩm Lowland" : item.Name.Trim(),
                UnitPrice = unitPrice,
                Quantity = qty,
                Size = sizeLabel,
                OptionsSummary = $"Size {sizeLabel}"
            });
        }

        if (orderItems.Count == 0)
        {
            ErrorMessage = "Danh sách sản phẩm trong giỏ hàng không hợp lệ.";
            return Page();
        }

        // Kiểm tra mã giảm giá qua EngagementHttpService (nếu khách có nhập)
        decimal discountAmount = CheckoutInput.DiscountAmount >= 0 ? CheckoutInput.DiscountAmount : 0m;
        if (!string.IsNullOrWhiteSpace(CheckoutInput.VoucherCode))
        {
            var voucherRes = await _engagementHttpService.ValidateVoucherAsync(new ValidateVoucherRequestViewModel
            {
                VoucherCode = CheckoutInput.VoucherCode.Trim().ToUpperInvariant(),
                OrderSubtotal = subtotal
            });

            if (voucherRes.IsSuccess && voucherRes.Data != null && voucherRes.Data.IsValid)
            {
                discountAmount = voucherRes.Data.DiscountAmount;
            }
        }

        var locationDesc = isPickup
            ? $"Nhận tại quán: {CheckoutInput.StoreName ?? "Lowland Nguyễn Huệ (Quận 1)"}"
            : $"Giao đến: {CheckoutInput.Address?.Trim()}";

        var noteParts = new List<string>
        {
            $"Người nhận: {CheckoutInput.FullName.Trim()} ({CheckoutInput.Phone.Trim()})",
            locationDesc
        };

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

        if (paymentMethod == "VNPAY")
        {
            TempData["SuccessMessage"] = $"Đã tạo đơn hàng #{createdOrder.OrderCode} thành công! Vui lòng quét mã QR VNPay hoặc bấm 'Mở cổng thanh toán VNPay' để hoàn tất thanh toán.";
            return RedirectToPage("/MyOrders", new { Created = true, PayOrderId = createdOrder.Id });
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

        var response = await _engagementHttpService.ValidateVoucherAsync(new ValidateVoucherRequestViewModel
        {
            VoucherCode = code.Trim().ToUpperInvariant(),
            OrderSubtotal = orderAmount
        });

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

    private async Task LoadAvailableVouchersAsync()
    {
        try
        {
            var res = await _engagementHttpService.GetActiveVouchersAsync();
            if (res.IsSuccess && res.Data != null)
            {
                AvailableVouchers = res.Data
                    .Where(v => v.IsValidNow)
                    .Take(5)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể tải danh sách voucher gợi ý");
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
