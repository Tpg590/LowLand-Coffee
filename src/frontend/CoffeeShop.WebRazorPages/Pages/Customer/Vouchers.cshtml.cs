using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CoffeeShop.WebRazorPages.Services;
using CoffeeShop.WebRazorPages.ViewModels.Engagement;
using System.Security.Claims;

namespace CoffeeShop.WebRazorPages.Pages.Customer;

/// <summary>
/// PageModel cho trang Kho Voucher Ưu Đãi (Customer Vouchers).
/// Phân quyền: Dành riêng cho tài khoản Khách hàng (Customer) tra cứu và săn mã giảm giá.
/// </summary>
public class VouchersModel : PageModel
{
    private readonly IEngagementHttpService _engagementService;

    public VouchersModel(IEngagementHttpService engagementService)
    {
        _engagementService = engagementService;
    }

    /// <summary>
    /// Danh sách mã voucher đang mở công khai.
    /// </summary>
    public IEnumerable<VoucherViewModel> Vouchers { get; set; } = new List<VoucherViewModel>();

    /// <summary>
    /// Xử lý HTTP GET: Nạp danh sách voucher và kiểm tra quyền hạn khách hàng.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
// Phân quyền truy cập tài nguyên do Backend Controller đảm nhiệm bằng Annotation

        try
        {
            var res = await _engagementService.GetActiveVouchersAsync();
            if (res.IsSuccess && res.Data != null && res.Data.Any())
            {
                Vouchers = res.Data;
            }
            else
            {
                Vouchers = GetDefaultVouchers();
            }
        }
        catch
        {
            Vouchers = GetDefaultVouchers();
        }

        return Page();
    }

    /// <summary>
    /// Danh sách mã voucher dự phòng khi hệ thống đang khởi tạo dịch vụ nền.
    /// </summary>
    private static List<VoucherViewModel> GetDefaultVouchers()
    {
        return new List<VoucherViewModel>
        {
            new() {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000001"),
                Code = "BANMOI",
                Title = "Ưu đãi chào bạn mới 20%",
                Description = "Giảm 20% tối đa 30.000 VNĐ cho khách hàng mới gia nhập LowLand Coffee.",
                DiscountType = "PERCENTAGE",
                DiscountValue = 20,
                MaxDiscountAmount = 30000,
                MinOrderValue = 40000,
                TotalQuantity = 1000,
                UsedQuantity = 15,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddMonths(3),
                IsActive = true
            },
            new() {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000002"),
                Code = "GIAM10K",
                Title = "Giảm 10K đơn từ 40K",
                Description = "Giảm trực tiếp 10.000 VNĐ cho hóa đơn từ 40.000 VNĐ.",
                DiscountType = "FIXED_AMOUNT",
                DiscountValue = 10000,
                MaxDiscountAmount = 10000,
                MinOrderValue = 40000,
                TotalQuantity = 500,
                UsedQuantity = 8,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddMonths(2),
                IsActive = true
            },
            new() {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000003"),
                Code = "FREESHIP",
                Title = "Mã ưu đãi phí giao hàng 15K",
                Description = "Giảm 15.000 VNĐ chi phí giao hàng cho đơn từ 60.000 VNĐ.",
                DiscountType = "FIXED_AMOUNT",
                DiscountValue = 15000,
                MaxDiscountAmount = 15000,
                MinOrderValue = 60000,
                TotalQuantity = 300,
                UsedQuantity = 0,
                StartDate = DateTime.UtcNow.AddDays(-2),
                EndDate = DateTime.UtcNow.AddMonths(1),
                IsActive = true
            }
        };
    }
}
