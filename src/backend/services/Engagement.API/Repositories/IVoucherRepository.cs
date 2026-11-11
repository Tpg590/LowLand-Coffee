using Engagement.API.Entities;

namespace Engagement.API.Repositories;

/// <summary>
/// Giao diện truy xuất dữ liệu cho hệ thống Voucher và lịch sử sử dụng.
/// </summary>
public interface IVoucherRepository
{
    /// <summary>
    /// Lấy danh sách voucher (có thể lọc chỉ lấy voucher còn hiệu lực).
    /// </summary>
    Task<IEnumerable<PromoVouchers>> GetVouchersAsync(bool? isActiveOnly = null);

    /// <summary>
    /// Tìm voucher theo mã định danh (ID).
    /// </summary>
    Task<PromoVouchers?> GetVoucherByIdAsync(Guid id);

    /// <summary>
    /// Tìm voucher theo mã code chữ.
    /// </summary>
    Task<PromoVouchers?> GetVoucherByCodeAsync(string code);

    /// <summary>
    /// Thêm mới một voucher vào cơ sở dữ liệu.
    /// </summary>
    Task<PromoVouchers> AddVoucherAsync(PromoVouchers voucher);

    /// <summary>
    /// Cập nhật thông tin voucher.
    /// </summary>
    Task UpdateVoucherAsync(PromoVouchers voucher);

    /// <summary>
    /// Xóa cứng voucher khỏi cơ sở dữ liệu.
    /// </summary>
    Task DeleteVoucherAsync(PromoVouchers voucher);

    /// <summary>
    /// Thêm bản ghi ghi nhận lượt sử dụng voucher.
    /// </summary>
    Task AddUsageAsync(PromoVoucherUsages usage);

    /// <summary>
    /// Đếm số lần một người dùng đã sử dụng một voucher cụ thể.
    /// </summary>
    Task<int> GetUserUsageCountAsync(Guid voucherId, Guid userId);

    /// <summary>
    /// Lấy danh sách ID các voucher mà một người dùng đã từng sử dụng.
    /// </summary>
    Task<IEnumerable<Guid>> GetUsedVoucherIdsByUserAsync(Guid userId);

    /// <summary>
    /// Lưu các thay đổi vào cơ sở dữ liệu.
    /// </summary>
    Task SaveChangesAsync();
}
