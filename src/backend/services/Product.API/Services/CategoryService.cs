using Product.API.DTOs;
using Product.API.Entities;
using Product.API.Repositories.Interfaces;
using Product.API.Services.Interfaces;

namespace Product.API.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<ApiResponse<IEnumerable<CategoryDto>>> GetAllCategoriesAsync(bool? isActiveOnly = null, CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(isActiveOnly, cancellationToken);
        var dtos = categories.Select(MapToDto);
        return ApiResponse<IEnumerable<CategoryDto>>.Success(dtos, "Lấy danh sách danh mục thành công");
    }

    public async Task<ApiResponse<CategoryDto>> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            return ApiResponse<CategoryDto>.Fail("Không tìm thấy danh mục", 404);
        }

        return ApiResponse<CategoryDto>.Success(MapToDto(category), "Lấy thông tin danh mục thành công");
    }

    public async Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var isDuplicate = await _categoryRepository.ExistsByNameAsync(request.Name, null, cancellationToken);
        if (isDuplicate)
        {
            return ApiResponse<CategoryDto>.Fail($"Danh mục '{request.Name}' đã tồn tại", 400);
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _categoryRepository.AddAsync(category, cancellationToken);
        return ApiResponse<CategoryDto>.Success(MapToDto(created), "Tạo danh mục mới thành công", 201);
    }

    public async Task<ApiResponse<CategoryDto>> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            return ApiResponse<CategoryDto>.Fail("Không tìm thấy danh mục cần cập nhật", 404);
        }

        var isDuplicate = await _categoryRepository.ExistsByNameAsync(request.Name, id, cancellationToken);
        if (isDuplicate)
        {
            return ApiResponse<CategoryDto>.Fail($"Tên danh mục '{request.Name}' đã được sử dụng", 400);
        }

        category.Name = request.Name.Trim();
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = request.IsActive;

        await _categoryRepository.UpdateAsync(category, cancellationToken);
        return ApiResponse<CategoryDto>.Success(MapToDto(category), "Cập nhật danh mục thành công");
    }

    public async Task<ApiResponse<bool>> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            return ApiResponse<bool>.Fail("Không tìm thấy danh mục cần xóa", 404);
        }

        var hasProducts = await _categoryRepository.HasProductsAsync(id, cancellationToken);
        if (hasProducts)
        {
            return ApiResponse<bool>.Fail("Không thể xóa danh mục đang chứa sản phẩm. Vui lòng chuyển hoặc xóa sản phẩm trước.", 400);
        }

        await _categoryRepository.DeleteAsync(category, cancellationToken);
        return ApiResponse<bool>.Success(true, "Xóa danh mục thành công");
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            DisplayOrder = category.DisplayOrder,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            ProductCount = category.Products?.Count(p => !p.IsDeleted) ?? 0
        };
    }
}
