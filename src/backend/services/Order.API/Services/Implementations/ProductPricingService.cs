using System.Text.Json;
using Order.API.DTOs;
using Order.API.Services.Interfaces;

namespace Order.API.Services.Implementations;

public class ProductPricingService : IProductPricingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductPricingService> _logger;

    public ProductPricingService(HttpClient httpClient, ILogger<ProductPricingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    private class ProductOptionDetail
    {
        public Guid Id { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal ExtraPrice { get; set; }
        public bool IsAvailable { get; set; }
    }

    private class ProductDetailResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public bool IsAvailable { get; set; }
        public List<ProductOptionDetail> Options { get; set; } = new();
    }

    private class ProductApiResponseWrapper
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public ProductDetailResponse? Data { get; set; }
    }

    public async Task<ProductPriceVerificationResult> VerifyAndCalculatePriceAsync(CreateOrderItemRequest item, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/products/{item.ProductId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Không thể lấy thông tin sản phẩm {ProductId} từ Product.API. Status: {Status}", item.ProductId, response.StatusCode);
                
                return new ProductPriceVerificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Không tìm thấy sản phẩm có mã {item.ProductId} trên Product.API (Status code: {(int)response.StatusCode}). Vui lòng kiểm tra lại."
                };
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var apiResult = JsonSerializer.Deserialize<ProductApiResponseWrapper>(content, jsonOptions);

            if (apiResult?.Data == null)
            {
                return new ProductPriceVerificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Không tìm thấy thông tin sản phẩm có mã {item.ProductId} trong hệ thống."
                };
            }

            var product = apiResult.Data;
            if (!product.IsAvailable)
            {
                return new ProductPriceVerificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Sản phẩm '{product.Name}' hiện đang tạm ngừng kinh doanh."
                };
            }

            decimal calculatedUnitPrice = product.BasePrice;

            // Tính phụ thu Size
            if (!string.IsNullOrWhiteSpace(item.Size))
            {
                var sizeOption = product.Options.FirstOrDefault(o =>
                    o.OptionType.Equals("SIZE", StringComparison.OrdinalIgnoreCase) &&
                    o.Name.Trim().Equals(item.Size.Trim(), StringComparison.OrdinalIgnoreCase));

                if (sizeOption != null)
                {
                    calculatedUnitPrice += sizeOption.ExtraPrice;
                }
            }

            // Tính phụ thu Toppings
            if (item.Toppings != null && item.Toppings.Any())
            {
                foreach (var topping in item.Toppings)
                {
                    if (string.IsNullOrWhiteSpace(topping)) continue;

                    var toppingOption = product.Options.FirstOrDefault(o =>
                        o.OptionType.Equals("TOPPING", StringComparison.OrdinalIgnoreCase) &&
                        o.Name.Trim().Equals(topping.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (toppingOption != null)
                    {
                        calculatedUnitPrice += toppingOption.ExtraPrice;
                    }
                }
            }

            // Luôn sử dụng đơn giá chuẩn được tính toán từ CSDL Server (Authoritative Server Pricing)
            if (item.UnitPrice > 0 && Math.Abs(item.UnitPrice - calculatedUnitPrice) > 0.01m)
            {
                _logger.LogInformation("Điều chỉnh đơn giá cho món {ProductName}: Giá client {ClientPrice} -> Giá chuẩn server {ServerPrice}",
                    product.Name, item.UnitPrice, calculatedUnitPrice);
            }

            return new ProductPriceVerificationResult
            {
                IsSuccess = true,
                ProductName = product.Name,
                VerifiedUnitPrice = calculatedUnitPrice
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi kết nối đến Product.API khi xác thực món {ProductId}", item.ProductId);

            return new ProductPriceVerificationResult
            {
                IsSuccess = false,
                ErrorMessage = $"Không thể kết nối đến máy chủ Product.API ({_httpClient.BaseAddress}). Vui lòng đảm bảo dịch vụ Product.API đang hoạt động."
            };
        }
    }
}
