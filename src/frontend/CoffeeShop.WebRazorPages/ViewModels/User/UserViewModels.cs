using System.ComponentModel.DataAnnotations;

namespace CoffeeShop.WebRazorPages.ViewModels.User;

public class CreateStaffViewModel
{
    [Required(ErrorMessage = "Họ và tên nhân viên là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Họ và tên không quá 100 ký tự")]
    [Display(Name = "Họ và tên nhân viên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ")]
    [MaxLength(255, ErrorMessage = "Email không quá 255 ký tự")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [MaxLength(20, ErrorMessage = "Số điện thoại không quá 20 ký tự")]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = "Admin@123";

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Display(Name = "Ảnh đại diện")]
    public string? AvatarUrl { get; set; }
}

public class UpdateUserViewModel
{
    [Required(ErrorMessage = "Họ và tên là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Họ và tên không quá 100 ký tự")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [MaxLength(20, ErrorMessage = "Số điện thoại không quá 20 ký tự")]
    [Display(Name = "Số điện thoại")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Display(Name = "Ảnh đại diện")]
    public string? AvatarUrl { get; set; }
}

public class UpdateUserStatusViewModel
{
    [Required(ErrorMessage = "Trạng thái hoạt động là bắt buộc")]
    public bool IsActive { get; set; }
}

public class UserQueryParametersViewModel
{
    public string? Search { get; set; }
    public string? RoleName { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class RoleViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

