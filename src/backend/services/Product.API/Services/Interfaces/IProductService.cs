using Product.API.DTOs;

namespace Product.API.Services.Interfaces;

public interface IProductService
{
    Task<ApiResponse<PagedResult<ProductDto>>> GetProductsAsync(ProductFilterRequest filter, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDetailDto>> GetProductDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDetailDto>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProductDetailDto>> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ToggleStockAsync(Guid id, bool? isAvailable = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
}
