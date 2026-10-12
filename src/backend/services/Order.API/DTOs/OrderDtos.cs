namespace Order.API.DTOs;

public class CreateOrderItemRequest
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

public class CreateOrderRequest
{
    public Guid? CustomerId { get; set; } // Dành cho Staff đặt hộ tại quầy
    public List<CreateOrderItemRequest> Items { get; set; } = new();
    public decimal DiscountAmount { get; set; } = 0.00m;
    public string? VoucherCode { get; set; }
    public string PaymentMethod { get; set; } = Entities.PaymentMethod.CASH;
    public string? Note { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CancelReason { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TotalItems { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
}

public class OrderItemDto
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

public class OrderDetailDto : OrderDto
{
    public List<OrderItemDto> Items { get; set; } = new();
    public List<OrderPaymentDto> Payments { get; set; } = new();
    public VnPayPaymentResponseDto? VnPayPayment { get; set; }
}

public class OrderFilterRequest
{
    public string? Status { get; set; }
    public string? OrderCode { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class CancelOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}
