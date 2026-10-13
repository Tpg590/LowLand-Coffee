using Product.API.DTOs;
using Product.API.Entities;
using Product.API.Repositories.Interfaces;
using Product.API.Services.Interfaces;

namespace Product.API.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<ApiResponse<PagedResult<ProductDto>>> GetProductsAsync(ProductFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _productRepository.GetFilteredAsync(filter, cancellationToken);

        var pagedResult = new PagedResult<ProductDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber > 0 ? filter.PageNumber : 1,
            PageSize = filter.PageSize > 0 ? filter.PageSize : 10
        };

        return ApiResponse<PagedResult<ProductDto>>.Success(pagedResult, "Lấy danh sách sản phẩm thành công");
    }

    public async Task<ApiResponse<ProductDetailDto>> GetProductDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, includeDeleted: false, cancellationToken);
        if (product == null)
        {
            return ApiResponse<ProductDetailDto>.Fail("Không tìm thấy sản phẩm", 404);
        }

        return ApiResponse<ProductDetailDto>.Success(MapToDetailDto(product), "Lấy chi tiết sản phẩm thành công");
    }

    public async Task<ApiResponse<ProductDetailDto>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null || !category.IsActive)
        {
            return ApiResponse<ProductDetailDto>.Fail("Danh mục không tồn tại hoặc đã bị vô hiệu hóa", 400);
        }

        var isDuplicate = await _productRepository.ExistsByNameAsync(request.Name, null, cancellationToken);
        if (isDuplicate)
        {
            return ApiResponse<ProductDetailDto>.Fail($"Sản phẩm '{request.Name}' đã tồn tại", 400);
        }

        var product = new Entities.Product
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            BasePrice = request.BasePrice,
            ImageUrl = request.ImageUrl?.Trim(),
            IsAvailable = request.IsAvailable,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (request.Options != null && request.Options.Count > 0)
        {
            foreach (var opt in request.Options)
            {
                product.Options.Add(new ProductOption
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    OptionType = opt.OptionType.Trim().ToUpperInvariant(),
                    Name = opt.Name.Trim(),
                    ExtraPrice = opt.ExtraPrice,
                    IsAvailable = opt.IsAvailable
                });
            }
        }

        await _productRepository.AddAsync(product, cancellationToken);

        // Fetch back with all includes
        var created = await _productRepository.GetByIdAsync(product.Id, false, cancellationToken);
        return ApiResponse<ProductDetailDto>.Success(MapToDetailDto(created ?? product), "Tạo sản phẩm mới thành công", 201);
    }

    public async Task<ApiResponse<ProductDetailDto>> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, includeDeleted: false, cancellationToken);
        if (product == null)
        {
            return ApiResponse<ProductDetailDto>.Fail("Không tìm thấy sản phẩm cần cập nhật", 404);
        }

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category == null)
        {
            return ApiResponse<ProductDetailDto>.Fail("Danh mục không tồn tại", 400);
        }

        var isDuplicate = await _productRepository.ExistsByNameAsync(request.Name, id, cancellationToken);
        if (isDuplicate)
        {
            return ApiResponse<ProductDetailDto>.Fail($"Tên sản phẩm '{request.Name}' đã được sử dụng", 400);
        }

        product.CategoryId = request.CategoryId;
        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.BasePrice = request.BasePrice;
        product.ImageUrl = request.ImageUrl?.Trim();
        product.IsAvailable = request.IsAvailable;
        product.UpdatedAt = DateTime.UtcNow;

        // Update Options
        if (request.Options != null)
        {
            product.Options.Clear();
            foreach (var opt in request.Options)
            {
                product.Options.Add(new ProductOption
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    OptionType = opt.OptionType.Trim().ToUpperInvariant(),
                    Name = opt.Name.Trim(),
                    ExtraPrice = opt.ExtraPrice,
                    IsAvailable = opt.IsAvailable
                });
            }
        }

        await _productRepository.UpdateAsync(product, cancellationToken);

        var updated = await _productRepository.GetByIdAsync(product.Id, false, cancellationToken);
        return ApiResponse<ProductDetailDto>.Success(MapToDetailDto(updated ?? product), "Cập nhật sản phẩm thành công");
    }

    public async Task<ApiResponse<bool>> ToggleStockAsync(Guid id, bool? isAvailable = null, CancellationToken cancellationToken = default)
    {
        var success = await _productRepository.ToggleStockAsync(id, isAvailable, cancellationToken);
        if (!success)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy sản phẩm", 404);
        }

        return ApiResponse<bool>.Success(true, "Cập nhật trạng thái kinh doanh thành công");
    }

    public async Task<ApiResponse<bool>> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var success = await _productRepository.SoftDeleteAsync(id, cancellationToken);
        if (!success)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy sản phẩm cần xóa", 404);
        }

        return ApiResponse<bool>.Success(true, "Xóa sản phẩm thành công");
    }

    private static ProductDto MapToDto(Entities.Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            Name = product.Name,
            Description = product.Description,
            BasePrice = product.BasePrice,
            ImageUrl = product.ImageUrl,
            IsAvailable = product.IsAvailable,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Options = product.Options.Select(MapOptionToDto).ToList()
        };
    }

    private static ProductDetailDto MapToDetailDto(Entities.Product product)
    {
        return new ProductDetailDto
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            Name = product.Name,
            Description = product.Description,
            BasePrice = product.BasePrice,
            ImageUrl = product.ImageUrl,
            IsAvailable = product.IsAvailable,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Options = product.Options.Select(MapOptionToDto).ToList(),
            Category = product.Category != null ? new CategoryDto
            {
                Id = product.Category.Id,
                Name = product.Category.Name,
                DisplayOrder = product.Category.DisplayOrder,
                IsActive = product.Category.IsActive,
                CreatedAt = product.Category.CreatedAt
            } : null
        };
    }

    private static ProductOptionDto MapOptionToDto(ProductOption opt)
    {
        return new ProductOptionDto
        {
            Id = opt.Id,
            OptionType = opt.OptionType,
            Name = opt.Name,
            ExtraPrice = opt.ExtraPrice,
            IsAvailable = opt.IsAvailable
        };
    }
}
