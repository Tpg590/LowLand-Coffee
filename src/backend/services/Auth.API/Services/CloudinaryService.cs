using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Auth.API.DTOs;
using Auth.API.Services.Interfaces;
using Auth.API.Settings;

namespace Auth.API.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public CloudinaryService(IOptions<CloudinarySettings> options)
    {
        var settings = options.Value;
        var apiSecret = settings.ApiSecret?.Trim() ?? string.Empty;

        if (apiSecret.Contains("CLOUDINARY_URL="))
        {
            var url = apiSecret.Replace("CLOUDINARY_URL=", string.Empty).Trim();
            _cloudinary = new Cloudinary(url);
        }
        else if (apiSecret.StartsWith("cloudinary://", StringComparison.OrdinalIgnoreCase))
        {
            _cloudinary = new Cloudinary(apiSecret);
        }
        else
        {
            var account = new Account(settings.CloudName?.Trim(), settings.ApiKey?.Trim(), apiSecret);
            _cloudinary = new Cloudinary(account);
        }

        _cloudinary.Api.Secure = true;
    }

    public async Task<ApiResponse<ImageUploadResultDto>> UploadImageAsync(IFormFile file, string folder = "lowland-coffee/avatars", CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return ApiResponse<ImageUploadResultDto>.Fail("Vui lòng chọn tệp hình ảnh để tải lên", 400);
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return ApiResponse<ImageUploadResultDto>.Fail("Kích thước tệp vượt quá giới hạn cho phép (tối đa 5MB)", 400);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            return ApiResponse<ImageUploadResultDto>.Fail("Định dạng tệp không hợp lệ. Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .webp, .gif", 400);
        }

        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            Transformation = new Transformation().Quality("auto").FetchFormat("auto")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (uploadResult.Error != null)
        {
            return ApiResponse<ImageUploadResultDto>.Fail($"Lỗi khi tải ảnh lên Cloudinary: {uploadResult.Error.Message}", 500);
        }

        var resultDto = new ImageUploadResultDto
        {
            Url = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty,
            PublicId = uploadResult.PublicId
        };

        return ApiResponse<ImageUploadResultDto>.Success(resultDto, "Tải ảnh lên Cloudinary thành công");
    }

    public async Task<ApiResponse<bool>> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return ApiResponse<bool>.Fail("PublicId của ảnh không hợp lệ", 400);
        }

        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);

        if (result.Result == "ok")
        {
            return ApiResponse<bool>.Success(true, "Xóa ảnh trên Cloudinary thành công");
        }

        return ApiResponse<bool>.Fail($"Không thể xóa ảnh: {result.Result}", 400);
    }
}
