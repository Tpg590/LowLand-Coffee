using Auth.API.DTOs;
using Microsoft.AspNetCore.Http;

namespace Auth.API.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserProfileDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserProfileDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserProfileDto>> UploadAvatarAsync(Guid userId, IFormFile file, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
