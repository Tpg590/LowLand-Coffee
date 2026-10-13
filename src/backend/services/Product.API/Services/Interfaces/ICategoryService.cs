using Product.API.DTOs;

namespace Product.API.Services.Interfaces;

public interface ICategoryService
{
    Task<ApiResponse<IEnumerable<CategoryDto>>> GetAllCategoriesAsync(bool? isActiveOnly = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<CategoryDto>> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<CategoryDto>> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
}
