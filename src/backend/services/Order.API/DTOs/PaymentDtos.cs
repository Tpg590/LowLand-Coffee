namespace Order.API.DTOs;

public class OrderPaymentDto
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

public class CreateVnPayPaymentRequest
{
    public Guid OrderId { get; set; }
    public string? ReturnUrl { get; set; }
}

public class VnPayPaymentResponseDto
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentUrl { get; set; } = string.Empty;
    public string? QrCodeBase64 { get; set; }
    public string ExpireDate { get; set; } = string.Empty;
}

public class CashPaymentConfirmRequest
{
    public string? TransactionCode { get; set; }
    public string? Note { get; set; }
}

public class VnPayIpnResponseDto
{
    public string RspCode { get; set; } = "99";
    public string Message { get; set; } = "Unknown error";
}

public class VnPayReturnResultDto
{
    public bool IsSuccess { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string? TransactionCode { get; set; }
    public decimal Amount { get; set; }
    public string Message { get; set; } = string.Empty;
}
