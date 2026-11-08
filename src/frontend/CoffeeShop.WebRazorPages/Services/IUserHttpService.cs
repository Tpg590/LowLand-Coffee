using CoffeeShop.WebRazorPages.ViewModels.Auth;
using CoffeeShop.WebRazorPages.ViewModels.Common;
using CoffeeShop.WebRazorPages.ViewModels.User;

namespace CoffeeShop.WebRazorPages.Services;

/// <summary>
/// Service HTTP giao tiếp với module Quản lý Người dùng thông qua API Gateway
/// </summary>
public interface IUserHttpService
{
    Task<ApiResponse<PagedResult<UserViewModel>>> GetUsersAsync(string accessToken, UserQueryParametersViewModel query, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserViewModel>> GetUserByIdAsync(string accessToken, Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserViewModel>> CreateStaffAsync(string accessToken, CreateStaffViewModel request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserViewModel>> UpdateUserAsync(string accessToken, Guid id, UpdateUserViewModel request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserViewModel>> UpdateUserStatusAsync(string accessToken, Guid id, bool isActive, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteUserAsync(string accessToken, Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<RoleViewModel>>> GetRolesAsync(string accessToken, CancellationToken cancellationToken = default);
}

