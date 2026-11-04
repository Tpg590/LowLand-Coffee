using Microsoft.AspNetCore.Mvc.RazorPages;
using CoffeeShop.WebRazorPages.Services;
using CoffeeShop.WebRazorPages.ViewModels.Engagement;

namespace CoffeeShop.WebRazorPages.Pages.Customer;

public class VouchersModel : PageModel
{
    private readonly IEngagementHttpService _engagementService;

    public VouchersModel(IEngagementHttpService engagementService)
    {
        _engagementService = engagementService;
    }

    public IEnumerable<VoucherViewModel> Vouchers { get; set; } = new List<VoucherViewModel>();

    public async Task OnGetAsync()
    {
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
    }

    private static List<VoucherViewModel> GetDefaultVouchers()
    {
        return new List<VoucherViewModel>
        {
            new() {
                Id = Guid.NewGuid(),
                Code = "LOWLAND20",
                Title = "Giảm 20% Cho Đơn Hàng Từ 100K",
                Description = "Áp dụng cho toàn bộ đồ uống Cà phê, Trà Sen Vàng và Freeze thơm ngon.",
                DiscountType = "PERCENTAGE",
                DiscountValue = 20,
                MaxDiscountAmount = 40000,
                MinOrderValue = 100000,
                TotalQuantity = 500,
                UsedQuantity = 142,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddMonths(2),
                IsActive = true
            },
            new() {
                Id = Guid.NewGuid(),
                Code = "WELCOME50",
                Title = "Giảm Ngay 50.000đ Cho Khách Hàng Mới",
                Description = "Quà làm quen cực chất dành cho thành viên mới của đại gia đình LowLand Coffee.",
                DiscountType = "FIXED_AMOUNT",
                DiscountValue = 50000,
                MaxDiscountAmount = null,
                MinOrderValue = 150000,
                TotalQuantity = 200,
                UsedQuantity = 85,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddMonths(1),
                IsActive = true
            },
            new() {
                Id = Guid.NewGuid(),
                Code = "FREESHIP",
                Title = "Miễn Phí Giao Hàng Toàn Quốc",
                Description = "Giảm trực tiếp 25.000đ phí vận chuyển cho đơn hàng giao tận nơi.",
                DiscountType = "FIXED_AMOUNT",
                DiscountValue = 25000,
                MaxDiscountAmount = null,
                MinOrderValue = 80000,
                TotalQuantity = 1000,
                UsedQuantity = 412,
                StartDate = DateTime.UtcNow.AddDays(-20),
                EndDate = DateTime.UtcNow.AddMonths(3),
                IsActive = true
            },
            new() {
                Id = Guid.NewGuid(),
                Code = "GOLDVIP15",
                Title = "Ưu Đãi Đặc Quyền Giảm 15%",
                Description = "Dành riêng cho khách hàng đạt hạng thẻ Vàng hoặc Kim Cương.",
                DiscountType = "PERCENTAGE",
                DiscountValue = 15,
                MaxDiscountAmount = 60000,
                MinOrderValue = 120000,
                TotalQuantity = 300,
                UsedQuantity = 54,
                StartDate = DateTime.UtcNow.AddDays(-2),
                EndDate = DateTime.UtcNow.AddMonths(2),
                IsActive = true
            }
        };
    }
}
