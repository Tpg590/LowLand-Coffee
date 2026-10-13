using System.ComponentModel.DataAnnotations;

namespace Product.API.DTOs;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ProductCount { get; set; }
}

public class CreateCategoryRequest
{
    [Required(ErrorMessage = "Tên danh mục không được để trống")]
    [MaxLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự")]
    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}

public class UpdateCategoryRequest
{
    [Required(ErrorMessage = "Tên danh mục không được để trống")]
    [MaxLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự")]
    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }
}
