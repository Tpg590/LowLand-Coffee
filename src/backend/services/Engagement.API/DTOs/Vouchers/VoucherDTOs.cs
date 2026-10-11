using System.ComponentModel.DataAnnotations;

namespace Engagement.API.DTOs.Vouchers;

public class VoucherDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "PERCENTAGE"; // PERCENTAGE hoặc FIXED_AMOUNT
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal MinOrderValue { get; set; }
    public int TotalQuantity { get; set; }
    public int UsedQuantity { get; set; }
    public int RemainingQuantity => Math.Max(0, TotalQuantity - UsedQuantity);
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsValidNow => IsActive && DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate && RemainingQuantity > 0;
    public DateTime CreatedAt { get; set; }
}

public class CreateVoucherRequest
{
    [Required(ErrorMessage = "Mã voucher không được để trống")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [RegularExpression("^(PERCENTAGE|FIXED_AMOUNT)$", ErrorMessage = "Loại giảm giá phải là PERCENTAGE hoặc FIXED_AMOUNT")]
    public string DiscountType { get; set; } = "PERCENTAGE";

    [Range(0.01, 10000000, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0")]
    public decimal DiscountValue { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public decimal MinOrderValue { get; set; } = 0.00m;

    [Range(1, 1000000, ErrorMessage = "Số lượng phát hành tối thiểu là 1")]
    public int TotalQuantity { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateVoucherRequest
{
    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [RegularExpression("^(PERCENTAGE|FIXED_AMOUNT)$", ErrorMessage = "Loại giảm giá phải là PERCENTAGE hoặc FIXED_AMOUNT")]
    public string DiscountType { get; set; } = "PERCENTAGE";

    [Range(0.01, 10000000, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0")]
    public decimal DiscountValue { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public decimal MinOrderValue { get; set; } = 0.00m;

    [Range(1, 1000000, ErrorMessage = "Số lượng phát hành tối thiểu là 1")]
    public int TotalQuantity { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; } = true;
}

public class ValidateVoucherRequest
{
    [Required(ErrorMessage = "Mã voucher không được để trống")]
    public string VoucherCode { get; set; } = string.Empty;

    [Range(0, 100000000, ErrorMessage = "Tổng giá trị đơn hàng không hợp lệ")]
    public decimal OrderSubtotal { get; set; }

    public Guid? UserId { get; set; }
}

public class ValidateVoucherResponse
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public Guid? VoucherId { get; set; }
    public string? Code { get; set; }
    public string? Title { get; set; }
    public string? DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalTotal { get; set; }
}

public class ApplyVoucherUsageRequest
{
    [Required]
    public Guid VoucherId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid OrderId { get; set; }

    [Range(0, 100000000)]
    public decimal DiscountAmount { get; set; }
}
