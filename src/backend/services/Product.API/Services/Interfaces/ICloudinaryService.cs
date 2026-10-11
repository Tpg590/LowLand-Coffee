using Microsoft.AspNetCore.Http;
using Product.API.DTOs;

namespace Product.API.Services.Interfaces;

public interface ICloudinaryService
{
    Task<ApiResponse<ImageUploadResultDto>> UploadImageAsync(IFormFile file, string folder = "lowland-coffee/products", CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default);
}
