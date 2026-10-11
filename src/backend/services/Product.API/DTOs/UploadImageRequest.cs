using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Product.API.DTOs;

public class UploadImageRequest
{
    [Required(ErrorMessage = "Vui lòng chọn tệp hình ảnh để tải lên")]
    public IFormFile File { get; set; } = null!;
}
