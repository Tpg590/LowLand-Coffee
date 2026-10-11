using Order.API.DTOs;

namespace Order.API.Services.Interfaces;

public class ProductPriceVerificationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal VerifiedUnitPrice { get; set; }
}

public interface IProductPricingService
{
    Task<ProductPriceVerificationResult> VerifyAndCalculatePriceAsync(CreateOrderItemRequest item, CancellationToken cancellationToken = default);
}
