using Auth.API.DTOs;

namespace Auth.API.Services.Interfaces;

public interface IUserService
{
    Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(UserQueryParameters query, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> CreateStaffAsync(CreateStaffRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> UpdateStatusAsync(Guid id, UpdateUserStatusRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<RoleDto>>> GetRolesAsync(CancellationToken cancellationToken = default);
}
