using Auth.API.DTOs;
using Microsoft.AspNetCore.Http;

namespace Auth.API.Services.Interfaces;

public interface ICloudinaryService
{
    Task<ApiResponse<ImageUploadResultDto>> UploadImageAsync(IFormFile file, string folder = "lowland-coffee/avatars", CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default);
}
