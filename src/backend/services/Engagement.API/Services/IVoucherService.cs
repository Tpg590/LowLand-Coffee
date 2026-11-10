using Engagement.API.DTOs.Vouchers;

namespace Engagement.API.Services;

/// <summary>
/// Giao diện định nghĩa các nghiệp vụ quản lý và thẩm định Voucher khuyến mãi.
/// </summary>
public interface IVoucherService
{
    /// <summary>
    /// Lấy danh sách voucher theo điều kiện trạng thái kích hoạt.
    /// </summary>
    Task<IEnumerable<VoucherDto>> GetAllVouchersAsync(bool? isActiveOnly = null);

    /// <summary>
    /// Lấy chi tiết thông tin voucher theo mã định danh (ID).
    /// </summary>
    Task<VoucherDto?> GetVoucherByIdAsync(Guid id);

    /// <summary>
    /// Tạo mới một mã voucher khuyến mãi.
    /// </summary>
    Task<VoucherDto> CreateVoucherAsync(CreateVoucherRequest request);

    /// <summary>
    /// Cập nhật thông tin cấu hình của voucher hiện có.
    /// </summary>
    Task<VoucherDto?> UpdateVoucherAsync(Guid id, UpdateVoucherRequest request);

    /// <summary>
    /// Xóa hoặc vô hiệu hóa voucher.
    /// </summary>
    Task<bool> DeleteVoucherAsync(Guid id);

    /// <summary>
    /// Thẩm định tính hợp lệ của mã voucher cho đơn hàng và tính số tiền giảm giá.
    /// </summary>
    Task<ValidateVoucherResponse> ValidateVoucherAsync(ValidateVoucherRequest request);

    /// <summary>
    /// Ghi nhận lượt sử dụng voucher khi đơn hàng thành công.
    /// </summary>
    Task<bool> ApplyVoucherUsageAsync(ApplyVoucherUsageRequest request);

    /// <summary>
    /// Lấy danh sách ID các voucher mà khách hàng đã từng sử dụng.
    /// </summary>
    Task<IEnumerable<Guid>> GetUsedVoucherIdsByUserAsync(Guid userId);
}
