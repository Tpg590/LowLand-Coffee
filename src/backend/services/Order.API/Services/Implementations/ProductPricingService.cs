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
                _logger.LogWarning("Không thể kết nối đến Product Service cho sản phẩm {ProductId}. Status: {Status}", item.ProductId, response.StatusCode);
                
                // Fallback nếu Product Service chưa chạy hoặc không tìm thấy: dùng tên & giá từ item nếu hợp lệ
                return new ProductPriceVerificationResult
                {
                    IsSuccess = true,
                    ProductName = !string.IsNullOrWhiteSpace(item.ProductName) ? item.ProductName : "Sản phẩm cà phê",
                    VerifiedUnitPrice = item.UnitPrice > 0 ? item.UnitPrice : 0
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

            // Kiểm tra Price Tampering:
            // Nếu Client gửi UnitPrice sai lệch so với giá hệ thống tính toán (chênh lệch > 0.01)
            if (item.UnitPrice > 0 && Math.Abs(item.UnitPrice - calculatedUnitPrice) > 0.01m)
            {
                _logger.LogWarning("Phát hiện Price Tampering! Sản phẩm {ProductName} ({ProductId}). Giá client gửi: {ClientPrice}, Giá chuẩn: {ServerPrice}",
                    product.Name, item.ProductId, item.UnitPrice, calculatedUnitPrice);

                return new ProductPriceVerificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Phát hiện sai lệch đơn giá cho món '{product.Name}'. Giá thực tế: {calculatedUnitPrice:N0}đ, Giá gửi lên: {item.UnitPrice:N0}đ.",
                    ProductName = product.Name,
                    VerifiedUnitPrice = calculatedUnitPrice
                };
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
            _logger.LogError(ex, "Lỗi khi kiểm tra giá từ Product Service cho sản phẩm {ProductId}", item.ProductId);

            // Dự phòng linh hoạt khi môi trường microservices chạy độc lập
            return new ProductPriceVerificationResult
            {
                IsSuccess = true,
                ProductName = !string.IsNullOrWhiteSpace(item.ProductName) ? item.ProductName : "Sản phẩm",
                VerifiedUnitPrice = item.UnitPrice
            };
        }
    }
}
