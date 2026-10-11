using System.ComponentModel.DataAnnotations;

namespace Product.API.DTOs;

public class ProductOptionDto
{
    public Guid Id { get; set; }
    public string OptionType { get; set; } = "SIZE";
    public string Name { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
    public bool IsAvailable { get; set; }
}

public class ProductOptionRequest
{
    [Required(ErrorMessage = "Loại tùy chọn không được để trống")]
    public string OptionType { get; set; } = "SIZE";

    [Required(ErrorMessage = "Tên tùy chọn không được để trống")]
    [MaxLength(50, ErrorMessage = "Tên tùy chọn tối đa 50 ký tự")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 10000000, ErrorMessage = "Giá phụ thu phải lớn hơn hoặc bằng 0")]
    public decimal ExtraPrice { get; set; } = 0.00m;

    public bool IsAvailable { get; set; } = true;
}

public class ProductDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ProductOptionDto> Options { get; set; } = new();
}

public class ProductDetailDto : ProductDto
{
    public CategoryDto? Category { get; set; }
    public List<ProductOptionDto> Sizes => Options.Where(o => o.OptionType.Equals("SIZE", StringComparison.OrdinalIgnoreCase)).ToList();
    public List<ProductOptionDto> Toppings => Options.Where(o => o.OptionType.Equals("TOPPING", StringComparison.OrdinalIgnoreCase)).ToList();
}

public class ProductFilterRequest
{
    public string? Keyword { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? IsAvailable { get; set; }
    public string? SortBy { get; set; } // name, price, date
    public bool IsDescending { get; set; } = false;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class CreateProductRequest
{
    [Required(ErrorMessage = "Mã danh mục không được để trống")]
    public Guid CategoryId { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    [MaxLength(150, ErrorMessage = "Tên sản phẩm tối đa 150 ký tự")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, 100000000, ErrorMessage = "Giá sản phẩm phải lớn hơn hoặc bằng 0")]
    public decimal BasePrice { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; } = true;

    public List<ProductOptionRequest>? Options { get; set; } = new();
}

public class UpdateProductRequest
{
    [Required(ErrorMessage = "Mã danh mục không được để trống")]
    public Guid CategoryId { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    [MaxLength(150, ErrorMessage = "Tên sản phẩm tối đa 150 ký tự")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0, 100000000, ErrorMessage = "Giá sản phẩm phải lớn hơn hoặc bằng 0")]
    public decimal BasePrice { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; }

    public List<ProductOptionRequest>? Options { get; set; } = new();
}

public class ToggleStockRequest
{
    public bool? IsAvailable { get; set; }
}
