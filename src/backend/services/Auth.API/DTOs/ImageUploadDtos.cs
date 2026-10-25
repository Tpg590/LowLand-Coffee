using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Auth.API.DTOs;

public class UploadImageRequest
{
    [Required(ErrorMessage = "Vui lòng chọn tệp hình ảnh để tải lên")]
    public IFormFile File { get; set; } = null!;
}

public class ImageUploadResultDto
{
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
}
