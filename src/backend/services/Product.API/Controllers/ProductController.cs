using Microsoft.AspNetCore.Mvc;
using Product.API.DTOs;
using Product.API.Services.Interfaces;

namespace Product.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ICloudinaryService _cloudinaryService;

    public ProductController(IProductService productService, ICloudinaryService cloudinaryService)
    {
        _productService = productService;
        _cloudinaryService = cloudinaryService;
    }

    /// <summary>
    /// Tải hình ảnh sản phẩm lên Cloudinary
    /// </summary>
    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<ImageUploadResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ImageUploadResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageRequest request, CancellationToken cancellationToken)
    {
        if (request == null || request.File == null)
        {
            return BadRequest(ApiResponse<ImageUploadResultDto>.Fail("Vui lòng chọn tệp hình ảnh để tải lên", 400));
        }

        var result = await _cloudinaryService.UploadImageAsync(request.File, "lowland-coffee/products", cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Xóa hình ảnh trên Cloudinary theo PublicId
    /// </summary>
    [HttpDelete("images/{publicId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteImage([FromRoute] string publicId, CancellationToken cancellationToken)
    {
        var result = await _cloudinaryService.DeleteImageAsync(publicId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Chức năng 16 & 18: View Product & Search/Filter Product (Có phân trang, tìm kiếm theo từ khóa, lọc theo danh mục, khoảng giá, trạng thái kinh doanh)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] ProductFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductsAsync(filter, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Chức năng 17: View Product Detail - Xem chi tiết một sản phẩm kèm Size, Topping và Danh mục
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductDetail([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.GetProductDetailAsync(id, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Chức năng 19: Create Product - Thêm món mới kèm các lựa chọn Size, Topping (Admin)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<ProductDetailDto>.Fail("Dữ liệu không hợp lệ", 400, errors));
        }

        var result = await _productService.CreateProductAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Chức năng 20: Update Product - Cập nhật thông tin món, giá, ảnh, size và topping (Admin)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct([FromRoute] Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<ProductDetailDto>.Fail("Dữ liệu không hợp lệ", 400, errors));
        }

        var result = await _productService.UpdateProductAsync(id, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Bật/Tắt trạng thái kinh doanh nhanh (Còn hàng / Hết hàng) (Staff/Admin)
    /// </summary>
    [HttpPatch("{id:guid}/toggle-stock")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleStock([FromRoute] Guid id, [FromBody] ToggleStockRequest? request, CancellationToken cancellationToken)
    {
        var result = await _productService.ToggleStockAsync(id, request?.IsAvailable, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    /// <summary>
    /// Chức năng 21: Delete Product - Xóa mềm sản phẩm khỏi Menu (IsDeleted = true) (Admin)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _productService.DeleteProductAsync(id, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
