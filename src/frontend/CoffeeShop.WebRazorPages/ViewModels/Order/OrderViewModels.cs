using System.ComponentModel.DataAnnotations;

namespace CoffeeShop.WebRazorPages.ViewModels.Order;

public class CreateOrderItemInputViewModel
{
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Size { get; set; }
    public List<string>? Toppings { get; set; }
    public string? Note { get; set; }
    public string? OptionsSummary { get; set; }
}

public class CreateOrderInputViewModel
{
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public List<CreateOrderItemInputViewModel> Items { get; set; } = new();
    public decimal DiscountAmount { get; set; } = 0m;
    public string? VoucherCode { get; set; }
    public string PaymentMethod { get; set; } = "CASH";
    public string? Note { get; set; }
    public string? ReturnUrl { get; set; }
}

public class OrderFilterViewModel
{
    public string? Status { get; set; }
    public string? OrderCode { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class OrderDtoViewModel
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string Status { get; set; } = "PENDING";
    public string? CancelReason { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalItems { get; set; }
    public string? ItemsSummary { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }

    // Computed Helpers cho View (Razor)
    public string NormalizedStatus => (Status ?? "PENDING").Trim().ToUpperInvariant();
    public string NormalizedPaymentStatus => (PaymentStatus ?? "PENDING").Trim().ToUpperInvariant();
    public string NormalizedPaymentMethod => (PaymentMethod ?? "CASH").Trim().ToUpperInvariant();

    public bool IsPaid => NormalizedPaymentStatus is "SUCCESS" or "PAID";
    public bool IsCancelled => NormalizedStatus == "CANCELLED";
    public bool IsCompleted => NormalizedStatus == "COMPLETED";
    public bool IsPending => NormalizedStatus == "PENDING";
    public bool IsProcessing => NormalizedStatus == "PROCESSING";
    public bool IsShipping => NormalizedStatus == "SHIPPING";

    /// <summary>
    /// Khách hàng được phép bấm nút Thanh toán khi đơn chưa thanh toán thành công và chưa bị hủy / hoàn thành
    /// </summary>
    public bool CanRetryPayment => !IsPaid && !IsCancelled && !IsCompleted;

    /// <summary>
    /// Khách hàng chỉ được phép hủy khi đơn đang ở trạng thái PENDING
    /// </summary>
    public bool CanCustomerCancel => IsPending;

    /// <summary>
    /// Nhân viên chỉ được bấm Hoàn thành khi đơn đang ở trạng thái SHIPPING và ĐÃ THU TIỀN (IsPaid == true)
    /// </summary>
    public bool CanCompleteOrder => IsShipping && IsPaid;

    public string StatusDisplayName => NormalizedStatus switch
    {
        "PENDING" => "Chờ xác nhận",
        "PROCESSING" => "Đang pha chế",
        "SHIPPING" => "Đang giao / Chờ nhận",
        "COMPLETED" => "Hoàn thành",
        "CANCELLED" => "Đã hủy",
        _ => Status
    };

    public string PaymentMethodDisplayName => NormalizedPaymentMethod switch
    {
        "VNPAY" => "VNPay QR",
        "PAYOS" => "PayOS QR",
        _ => "Tiền mặt (COD)"
    };

    public string PaymentStatusDisplayName => IsPaid
        ? "Đã thanh toán"
        : (NormalizedPaymentStatus == "FAILED" ? "Thanh toán chưa hoàn tất" : "Chưa thanh toán");
}

public class OrderItemViewModel
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? OptionsSummary { get; set; }
    public decimal Subtotal { get; set; }
}

public class OrderPaymentViewModel
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionCode { get; set; }
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VnPayPaymentResponseViewModel
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentUrl { get; set; } = string.Empty;
    public string? QrCodeBase64 { get; set; }
    public string ExpireDate { get; set; } = string.Empty;
}

public class OrderDetailViewModel : OrderDtoViewModel
{
    public List<OrderItemViewModel> Items { get; set; } = new();
    public List<OrderPaymentViewModel> Payments { get; set; } = new();
    public VnPayPaymentResponseViewModel? VnPayPayment { get; set; }
}

public class VnPayReturnResultViewModel
{
    public bool IsSuccess { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string? TransactionCode { get; set; }
    public decimal Amount { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class UpdateOrderStatusInputViewModel
{
    [Required]
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class CancelOrderInputViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập lý do hủy đơn hàng.")]
    public string Reason { get; set; } = string.Empty;
}

public class CashPaymentConfirmInputViewModel
{
    public string? TransactionCode { get; set; }
    public string? Note { get; set; }
}

public class PosProductItemViewModel
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class PosCategoryItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class CheckoutFormInputViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên người nhận.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại liên hệ.")]
    [RegularExpression(@"^(0|\+84)[0-9]{8,10}$", ErrorMessage = "Số điện thoại không hợp lệ (gồm 9-11 chữ số, bắt đầu bằng 0 hoặc +84).")]
    public string Phone { get; set; } = string.Empty;

    public string OrderType { get; set; } = "delivery";

    public string? Address { get; set; }

    public string? StoreName { get; set; }

    public string? Note { get; set; }

    public string PaymentMethod { get; set; } = "CASH";

    public string? VoucherCode { get; set; }

    public decimal DiscountAmount { get; set; } = 0m;

    public int PointsToRedeem { get; set; } = 0;

    public string CartJson { get; set; } = "[]";
}

